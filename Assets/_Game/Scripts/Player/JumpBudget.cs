using UnityEngine;

namespace HealthyRace.Player
{
    /// <summary>
    /// Orçamento de pulo derivado da física real em vigor, nunca de literais.
    /// Todos os termos vêm do runtime: <see cref="Physics2D.gravity"/>, o
    /// <c>gravityScale</c> aplicado ao Rigidbody2D do jogador, a velocidade de pulo
    /// e de movimento do <see cref="PlayerMovement"/> (já compostas com os
    /// multiplicadores ativos) e o passo de física <see cref="Time.fixedDeltaTime"/>.
    ///
    /// O modelo é o do integrador da Unity (Euler semi-implícito), não o contínuo:
    ///
    /// <code>
    /// v -= g * dt;
    /// y += v * dt;
    /// </code>
    ///
    /// A fórmula contínua v²/(2g) superestima o ápice em aproximadamente
    /// v₀·dt/2 — com v₀ = 12 e dt = 0.02 isso são 0.12 m, ou 6% do salto. Era essa
    /// a divergência entre a documentação e o jogo. Geometria, HUD e testes
    /// consomem este tipo, que por sua vez lê a física viva.
    /// </summary>
    public readonly struct JumpBudget
    {
        /// <summary>Folga vertical descontada do ápice para definir um degrau seguro.</summary>
        public const float StepSafetyMargin = 0.2f;

        /// <summary>Folga horizontal descontada do alcance para definir um vão seguro.</summary>
        public const float GapSafetyMargin = 0.67f;

        private const int MaximumSimulationSteps = 4096;

        /// <summary>Gravidade efetiva em m/s², sempre positiva.</summary>
        public readonly float Gravity;

        /// <summary>Velocidade vertical efetiva do pulo, já com multiplicadores.</summary>
        public readonly float JumpVelocity;

        /// <summary>Velocidade horizontal efetiva, já com multiplicadores.</summary>
        public readonly float MoveSpeed;

        /// <summary>Passo de integração da física.</summary>
        public readonly float FixedDeltaTime;

        public JumpBudget(float gravity, float jumpVelocity, float moveSpeed, float fixedDeltaTime)
        {
            Gravity = Mathf.Max(0.0001f, gravity);
            JumpVelocity = Mathf.Max(0f, jumpVelocity);
            MoveSpeed = Mathf.Max(0f, moveSpeed);
            FixedDeltaTime = Mathf.Clamp(fixedDeltaTime, 0.0001f, 0.5f);
        }

        /// <summary>
        /// Ápice previsto pela fórmula contínua. Mantido só para comparação: o
        /// jogador nunca alcança esta altura, porque a física é integrada em passos.
        /// </summary>
        public float IdealApex => (JumpVelocity * JumpVelocity) / (2f * Gravity);

        /// <summary>Altura máxima realmente alcançada, conforme o integrador discreto.</summary>
        public float Apex => Simulate(out _);

        /// <summary>Tempo no ar até retornar à altura de partida.</summary>
        public float AirTime
        {
            get
            {
                Simulate(out var airTime);
                return airTime;
            }
        }

        /// <summary>Distância horizontal percorrida durante o pulo em velocidade máxima.</summary>
        public float Reach => MoveSpeed * AirTime;

        /// <summary>Maior degrau que o jogador vence com folga.</summary>
        public float SafeStep => Mathf.Max(0f, Apex - StepSafetyMargin);

        /// <summary>Maior vão que o jogador vence com folga.</summary>
        public float SafeGap => Mathf.Max(0f, Reach - GapSafetyMargin);

        public bool CanClear(float rise, float gap)
        {
            const float epsilon = 0.001f;
            return rise <= SafeStep + epsilon && gap <= SafeGap + epsilon;
        }

        /// <summary>
        /// Reproduz o arco do pulo com o mesmo integrador da Unity e devolve o ápice,
        /// escrevendo em <paramref name="airTime"/> o tempo até voltar à altura inicial.
        /// </summary>
        private float Simulate(out float airTime)
        {
            var dt = FixedDeltaTime;
            var velocity = JumpVelocity;
            var height = 0f;
            var peak = 0f;
            airTime = 0f;

            for (var step = 0; step < MaximumSimulationSteps; step++)
            {
                velocity -= Gravity * dt;
                height += velocity * dt;
                airTime += dt;

                if (height > peak)
                {
                    peak = height;
                }

                if (height <= 0f)
                {
                    break;
                }
            }

            return peak;
        }

        /// <summary>
        /// Monta o orçamento a partir dos componentes vivos de um jogador.
        /// Retorna um orçamento zerado se as referências não existirem.
        /// </summary>
        public static JumpBudget For(PlayerMovement movement)
        {
            if (movement == null)
            {
                return default;
            }

            var rigidbody = movement.GetComponent<Rigidbody2D>();
            var gravityScale = rigidbody != null ? rigidbody.gravityScale : 1f;

            return FromPhysics(
                Physics2D.gravity.y,
                gravityScale,
                movement.JumpVelocity * movement.JumpMultiplier,
                movement.MoveSpeed * movement.MovementMultiplier
            );
        }

        /// <summary>Monta o orçamento a partir de valores brutos de física.</summary>
        public static JumpBudget FromPhysics(
            float gravityY,
            float gravityScale,
            float effectiveJumpVelocity,
            float effectiveMoveSpeed,
            float fixedDeltaTime = 0f
        )
        {
            var dt = fixedDeltaTime > 0f ? fixedDeltaTime : Time.fixedDeltaTime;
            return new JumpBudget(
                Mathf.Abs(gravityY) * Mathf.Abs(gravityScale),
                effectiveJumpVelocity,
                effectiveMoveSpeed,
                dt
            );
        }

        /// <summary>
        /// <c>gravityScale</c> necessário para que o ápice **medido** seja
        /// <paramref name="targetApex"/>. Resolve numericamente porque o ápice
        /// discreto não tem inversa fechada. É assim que o nível declara a intenção
        /// de projeto em vez de fixar um número.
        /// </summary>
        public static float GravityScaleForApex(
            float gravityY,
            float jumpVelocity,
            float targetApex,
            float fixedDeltaTime = 0f
        )
        {
            var gravityMagnitude = Mathf.Abs(gravityY);
            if (gravityMagnitude < 0.0001f || targetApex < 0.0001f || jumpVelocity < 0.0001f)
            {
                return 1f;
            }

            var dt = fixedDeltaTime > 0f ? fixedDeltaTime : Time.fixedDeltaTime;

            // O ápice decresce monotonicamente com a escala, então uma busca binária
            // converge. O palpite contínuo é o ponto de partida natural.
            var low = 0.01f;
            var high = Mathf.Max(
                1f,
                (jumpVelocity * jumpVelocity) / (2f * targetApex * gravityMagnitude) * 4f
            );

            for (var iteration = 0; iteration < 64; iteration++)
            {
                var middle = (low + high) * 0.5f;
                var apex = new JumpBudget(gravityMagnitude * middle, jumpVelocity, 0f, dt).Apex;

                if (apex > targetApex)
                {
                    low = middle;
                }
                else
                {
                    high = middle;
                }
            }

            return (low + high) * 0.5f;
        }

        public override string ToString()
        {
            return $"g={Gravity:F2} v0={JumpVelocity:F2} apex={Apex:F3} (ideal {IdealApex:F3}) "
                + $"reach={Reach:F3} safeStep={SafeStep:F3} safeGap={SafeGap:F3}";
        }
    }
}
