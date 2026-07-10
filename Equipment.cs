using System;
using System.Collections.Generic;

namespace MyGame
{
    public class Equipment
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; } // "Head", "Chest", "Gauntlet", "Leg", "Shoes", "Weapon", "Health", "Misc"
        public string Tier { get; set; } // "S", "A", "B", "C", "D"
        public string StatType { get; set; } // "MaxHP", "Damage", "Speed", "Armor", "Magnet", "Cooldown", "Growth", "Greed", "Luck", "Regen", "Area", "Duration", "ProjSpeed"
        public float StatValue { get; set; }
        public int Cost { get; set; }
        public string Description { get; set; }
    }

    public static class EquipmentDatabase
    {
        public static List<Equipment> Items { get; private set; }
        private static Dictionary<string, Equipment> _itemsById;

        static EquipmentDatabase()
        {
            Items = new List<Equipment>();
            _itemsById = new Dictionary<string, Equipment>();

            string[] categories = { "Head", "Chest", "Gauntlet", "Leg", "Shoes", "Weapon", "Health", "Misc" };
            
            // Adjectives based on tier
            string[] sAdjectives = { "Than Thoai", "Hoang Kim", "De Vuong", "Vo Song" };
            string[] aAdjectives = { "Anh Hung", "Tinh Anh", "Vien Co", "U Uan" };
            string[] bAdjectives = { "Quy Hiem", "Kien Co", "Sac Ben", "Chung Sy" };
            string[] cAdjectives = { "Pho Thong", "Binh Thuong", "Hieu Ky", "Dung Nhe" };
            string[] dAdjectives = { "Co Ban", "Phe Lieu", "Giay Gai", "Hu Hong" };

            // Nouns for categories
            string[][] nouns = new string[8][];
            nouns[0] = new string[] { "Non Sat", "Mu Rong", "Vuong Mien", "Mu Chon", "Mat Na" }; // Head
            nouns[1] = new string[] { "Ao Giap", "Giap Tam", "Ao Choang", "Long Bao", "Giap Ngoc" }; // Chest
            nouns[2] = new string[] { "Gang Tay", "Giap Tay", "Bao Tay", "Co Tay", "Khop Khien" }; // Gauntlet
            nouns[3] = new string[] { "Giap Chan", "Xa Cu", "Ung Chan", "Chan De", "Bao Ong" }; // Leg
            nouns[4] = new string[] { "Giay Da", "Hai Rong", "De Biet", "Giay Vao", "Giay Chay" }; // Shoes
            nouns[5] = new string[] { "Kiem Sat", "Do Chuy", "Xich Kiem", "Song Dao", "Cung Ten" }; // Weapon
            nouns[6] = new string[] { "Binh Mau", "Duoc Thao", "Nuoc Thanh", "Ruou De", "Linh Dan" }; // Health
            nouns[7] = new string[] { "Bua Ho Menh", "Nhan Co", "Huy Hieu", "Tach Tra", "Khien Nho" }; // Misc

            Random rand = new Random(42); // Seeded for reproducibility

            foreach (var cat in categories)
            {
                int catIdx = Array.IndexOf(categories, cat);
                
                // For each category, generate 20 items:
                // 0-1 (S Tier)
                // 2-5 (A Tier)
                // 6-10 (B Tier)
                // 11-15 (C Tier)
                // 16-19 (D Tier)
                for (int i = 0; i < 20; i++)
                {
                    string tier;
                    string adj;
                    int cost;
                    float multiplier;
                    
                    if (i < 2)
                    {
                        tier = "S";
                        adj = sAdjectives[rand.Next(sAdjectives.Length)];
                        cost = rand.Next(800, 1001);
                        multiplier = 1.0f;
                    }
                    else if (i < 6)
                    {
                        tier = "A";
                        adj = aAdjectives[rand.Next(aAdjectives.Length)];
                        cost = rand.Next(400, 601);
                        multiplier = 0.5f;
                    }
                    else if (i < 11)
                    {
                        tier = "B";
                        adj = bAdjectives[rand.Next(bAdjectives.Length)];
                        cost = rand.Next(200, 301);
                        multiplier = 0.25f;
                    }
                    else if (i < 16)
                    {
                        tier = "C";
                        adj = cAdjectives[rand.Next(cAdjectives.Length)];
                        cost = rand.Next(100, 151);
                        multiplier = 0.12f;
                    }
                    else
                    {
                        tier = "D";
                        adj = dAdjectives[rand.Next(dAdjectives.Length)];
                        cost = rand.Next(30, 51);
                        multiplier = 0.05f;
                    }

                    string noun = nouns[catIdx][rand.Next(nouns[catIdx].Length)];
                    string name = $"{noun} {adj} (+{i+1})";
                    string id = $"{cat.ToLower()}_{tier.ToLower()}_{i}";

                    // Assign stat type based on category
                    string statType = "MaxHP";
                    float baseVal = 0.1f;
                    string formattedVal = "";

                    switch (cat)
                    {
                        case "Head":
                            // Head: MaxHP, Cooldown, Armor
                            int headSel = rand.Next(3);
                            if (headSel == 0) { statType = "MaxHP"; baseVal = 50f; formattedVal = $"+{(int)(baseVal * multiplier)} HP"; }
                            else if (headSel == 1) { statType = "Cooldown"; baseVal = -0.15f; formattedVal = $"-{(int)(Math.Abs(baseVal * multiplier) * 100)}% Hoi chieu"; }
                            else { statType = "Armor"; baseVal = 3.0f; formattedVal = $"+{(baseVal * multiplier):0.0} Giap"; }
                            break;

                        case "Chest":
                            // Chest: MaxHP, Armor, Regen
                            int chestSel = rand.Next(3);
                            if (chestSel == 0) { statType = "MaxHP"; baseVal = 60f; formattedVal = $"+{(int)(baseVal * multiplier)} HP"; }
                            else if (chestSel == 1) { statType = "Armor"; baseVal = 4.0f; formattedVal = $"+{(baseVal * multiplier):0.0} Giap"; }
                            else { statType = "Regen"; baseVal = 1.5f; formattedVal = $"+{(baseVal * multiplier):0.00} HP/giay"; }
                            break;

                        case "Gauntlet":
                            // Gauntlet: Damage, Cooldown, Area
                            int gauntletSel = rand.Next(3);
                            if (gauntletSel == 0) { statType = "Damage"; baseVal = 0.30f; formattedVal = $"+{(int)(baseVal * multiplier * 100)}% Sat thuong"; }
                            else if (gauntletSel == 1) { statType = "Cooldown"; baseVal = -0.15f; formattedVal = $"-{(int)(Math.Abs(baseVal * multiplier) * 100)}% Hoi chieu"; }
                            else { statType = "Area"; baseVal = 0.30f; formattedVal = $"+{(int)(baseVal * multiplier * 100)}% Kich thuoc"; }
                            break;

                        case "Leg":
                            // Leg: Speed, Armor, Luck
                            int legSel = rand.Next(3);
                            if (legSel == 0) { statType = "Speed"; baseVal = 0.15f; formattedVal = $"+{(int)(baseVal * multiplier * 100)}% Toc do"; }
                            else if (legSel == 1) { statType = "Armor"; baseVal = 2.0f; formattedVal = $"+{(baseVal * multiplier):0.0} Giap"; }
                            else { statType = "Luck"; baseVal = 0.40f; formattedVal = $"+{(int)(baseVal * multiplier * 100)}% May man"; }
                            break;

                        case "Shoes":
                            // Shoes: Speed, Magnet
                            int shoesSel = rand.Next(2);
                            if (shoesSel == 0) { statType = "Speed"; baseVal = 0.20f; formattedVal = $"+{(int)(baseVal * multiplier * 100)}% Toc do"; }
                            else { statType = "Magnet"; baseVal = 0.50f; formattedVal = $"+{(int)(baseVal * multiplier * 100)}% Pham vi nhat"; }
                            break;

                        case "Weapon":
                            // Weapon: Damage, ProjSpeed, Area
                            int weaponSel = rand.Next(3);
                            if (weaponSel == 0) { statType = "Damage"; baseVal = 0.35f; formattedVal = $"+{(int)(baseVal * multiplier * 100)}% Sat thuong"; }
                            else if (weaponSel == 1) { statType = "ProjSpeed"; baseVal = 0.30f; formattedVal = $"+{(int)(baseVal * multiplier * 100)}% Toc do dan"; }
                            else { statType = "Area"; baseVal = 0.30f; formattedVal = $"+{(int)(baseVal * multiplier * 100)}% Kich thuoc"; }
                            break;

                        case "Health":
                            // Health: MaxHP, Regen
                            int hpSel = rand.Next(2);
                            if (hpSel == 0) { statType = "MaxHP"; baseVal = 40f; formattedVal = $"+{(int)(baseVal * multiplier)} HP"; }
                            else { statType = "Regen"; baseVal = 1.0f; formattedVal = $"+{(baseVal * multiplier):0.00} HP/giay"; }
                            break;

                        case "Misc":
                            // Misc: Growth, Greed, Luck, Duration
                            int miscSel = rand.Next(4);
                            if (miscSel == 0) { statType = "Growth"; baseVal = 0.40f; formattedVal = $"+{(int)(baseVal * multiplier * 100)}% Kinh nghiem"; }
                            else if (miscSel == 1) { statType = "Greed"; baseVal = 0.50f; formattedVal = $"+{(int)(baseVal * multiplier * 100)}% Vang"; }
                            else if (miscSel == 2) { statType = "Luck"; baseVal = 0.40f; formattedVal = $"+{(int)(baseVal * multiplier * 100)}% May man"; }
                            else { statType = "Duration"; baseVal = 0.40f; formattedVal = $"+{(int)(baseVal * multiplier * 100)}% Thoi gian duy tri"; }
                            break;
                    }

                    float finalVal = baseVal * multiplier;

                    var eq = new Equipment
                    {
                        Id = id,
                        Name = name,
                        Category = cat,
                        Tier = tier,
                        StatType = statType,
                        StatValue = finalVal,
                        Cost = cost,
                        Description = $"Trang bi {tier} tang {formattedVal} cho tran dau ke."
                    };

                    Items.Add(eq);
                    _itemsById[id] = eq;
                }
            }
        }

        public static Equipment GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _itemsById.TryGetValue(id, out var item) ? item : null;
        }

        public static List<Equipment> GetByCategory(string category)
        {
            var list = new List<Equipment>();
            foreach (var item in Items)
            {
                if (item.Category == category) list.Add(item);
            }
            return list;
        }
    }
}
