using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HealthyRace.Core
{
    /// <summary>
    /// Progresso salvo em JSON (<c>persistentDataPath/progresso.json</c>): fases concluídas
    /// e melhor tempo de cada fase. As opções de áudio continuam em <c>PlayerPrefs</c>.
    /// Arquivo corrompido ou de formato desconhecido vira progresso vazio, com aviso.
    /// </summary>
    public static class SaveGame
    {
        public const int FormatVersion = 1;
        public const string FileName = "progresso.json";

        private static SaveData _data;

        /// <summary>Caminho do arquivo; testes podem trocar por um temporário.</summary>
        public static string FilePath { get; set; }

        private static string Path =>
            string.IsNullOrEmpty(FilePath)
                ? System.IO.Path.Combine(Application.persistentDataPath, FileName)
                : FilePath;

        /// <summary>
        /// Em modo batch (testes) só grava em disco se <see cref="FilePath"/> foi definido:
        /// as corridas do robô não podem mexer no progresso de quem joga na máquina.
        /// </summary>
        public static bool WritesToDisk =>
            !Application.isBatchMode || !string.IsNullOrEmpty(FilePath);

        public static SaveData Data
        {
            get
            {
                _data ??= Load();
                return _data;
            }
        }

        public static bool IsCompleted(int level) => Find(level)?.Completed ?? false;

        /// <summary>Melhor tempo da fase em segundos, ou <c>null</c> se ninguém concluiu.</summary>
        public static float? BestTime(int level)
        {
            var record = Find(level);
            return record != null && record.BestTime > 0f ? record.BestTime : null;
        }

        /// <summary>Maior fase já liberada: a seguinte à maior concluída (mínimo 1).</summary>
        public static int HighestUnlocked(int levelCount)
        {
            var highest = 1;
            foreach (var record in Data.Levels)
            {
                if (record.Completed)
                {
                    highest = Mathf.Max(highest, record.Level + 1);
                }
            }

            return Mathf.Clamp(highest, 1, Mathf.Max(1, levelCount));
        }

        /// <summary>Registra uma chegada; devolve verdadeiro se for recorde.</summary>
        public static bool RecordFinish(int level, float seconds)
        {
            if (level < 1 || seconds <= 0f)
            {
                return false;
            }

            var record = Find(level);
            if (record == null)
            {
                record = new LevelRecord { Level = level };
                Data.Levels.Add(record);
            }

            record.Completed = true;
            var isRecord = record.BestTime <= 0f || seconds < record.BestTime;
            if (isRecord)
            {
                record.BestTime = seconds;
            }

            Save();
            return isRecord;
        }

        public static void Save()
        {
            if (!WritesToDisk)
            {
                return;
            }

            try
            {
                var directory = System.IO.Path.GetDirectoryName(Path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // grava num temporário e troca: queda no meio não corrompe o progresso
                var temp = Path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(Data, true));
                if (File.Exists(Path))
                {
                    File.Delete(Path);
                }

                File.Move(temp, Path);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"SaveGame: progresso não salvo ({exception.Message}).");
            }
        }

        /// <summary>Esquece o que está em memória (o próximo acesso relê o arquivo).</summary>
        public static void Reload()
        {
            _data = null;
        }

        private static LevelRecord Find(int level)
        {
            foreach (var record in Data.Levels)
            {
                if (record.Level == level)
                {
                    return record;
                }
            }

            return null;
        }

        private static SaveData Load()
        {
            if (!WritesToDisk)
            {
                return new SaveData();
            }

            try
            {
                if (File.Exists(Path))
                {
                    var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(Path));
                    if (data != null && data.Format == FormatVersion)
                    {
                        data.Levels ??= new List<LevelRecord>();
                        return data;
                    }

                    Debug.LogWarning("SaveGame: formato desconhecido; começando do zero.");
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"SaveGame: progresso ilegível ({exception.Message}).");
            }

            return new SaveData();
        }
    }

    [Serializable]
    public sealed class SaveData
    {
        public int Format = SaveGame.FormatVersion;
        public List<LevelRecord> Levels = new();
    }

    [Serializable]
    public sealed class LevelRecord
    {
        public int Level;
        public bool Completed;
        public float BestTime;
    }
}
