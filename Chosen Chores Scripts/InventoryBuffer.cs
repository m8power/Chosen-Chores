using System.Collections.Generic;

public static class InventoryBuffer
{
    // Хранит ID типов предметов (1 - Фонарик, 2 - Аптечка и т.д.)
    public static List<int> StoredItemIDs = new List<int>();

    public static void Clear() => StoredItemIDs.Clear();
}