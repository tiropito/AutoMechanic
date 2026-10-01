using System;
using System.Collections.Generic;

namespace AutoMechanic.Core
{
    /// <summary>
    /// Полная структура сохранений игры.
    /// Сериализуется в JSON и хранится в YG2.saves.am_data.
    /// </summary>
    [Serializable]
    public class GameSaveData
    {
        // ===== Экономика =====
        public int money = 200;

        // ===== Язык =====
        public string lang = "ru";

        // ===== Инвентарь =====
        public List<string> invIds = new List<string>();
        public List<int> invCounts = new List<int>();

        // ===== Коллекция =====
        public List<string> repairedIds = new List<string>();

        // ===== Слоты =====
        public int slotCount = 3;
        public List<string> slotCars = new List<string>();
        public List<bool> slotBonus = new List<bool>();
        public List<float> slotBonusTime = new List<float>();
        public List<float> slotEmptyTime = new List<float>();
        public List<float> slotRefill = new List<float>();

        // ===== Апгрейды =====
        public int upgSlots = 0;
        public int upgBays = 0;
    }
}