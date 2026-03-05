using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;

[CreateAssetMenu(fileName = "CardSprites", menuName = "ScriptableObjects/PokerCollection")]
public class PokerCollection : ScriptableObject
{
    public List<PokerDeck> PokerDecks;

    [MenuItem("Tools/Auto Import Poker From Sheets")]
    public static void ImportPokerFromSheets()
    {
        string[] guids = AssetDatabase.FindAssets("t:PokerCollection");
        if (guids.Length == 0) return;
        PokerCollection collection = AssetDatabase.LoadAssetAtPath<PokerCollection>(AssetDatabase.GUIDToAssetPath(guids[0]));

        string folderPath = "Assets/Textures/PokerCards";
        string[] filePaths = Directory.GetFiles(folderPath, "Deck*.png");

        collection.PokerDecks.Clear();

        foreach (var path in filePaths)
        {
            PokerDeck newDeck = new PokerDeck();
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (System.Enum.TryParse(fileName, out PokerStyle style)) newDeck.PokerStyle = style;

            // 先初始化一個空的牌組 (這會幫你建立 52 個帶有正確 Suit/Rank 的物件)
            newDeck.InitializeDeck();

            // 抓取所有 Sprite 並進行「自然排序」
            List<Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .Where(s => !s.name.Contains("Back"))
                .OrderBy(s => ExtractNumber(s.name)) // 使用自訂的數字提取函數排序
                .ToList();

            // 尋找牌背
            var backSprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault(s => s.name.Contains("Back"));
            newDeck.BackSprite = backSprite;

            // 對應填入
            for (int i = 0; i < sprites.Count && i < newDeck.pokerDatas.Count; i++)
            {
                newDeck.pokerDatas[i].FrontSprite = sprites[i];
            }

            collection.PokerDecks.Add(newDeck);
        }

        EditorUtility.SetDirty(collection);
        AssetDatabase.SaveAssets();
        Debug.Log("撲克牌與花色/點數已自動對應完成！");
    }
    /// <summary>
    /// 提取編號
    /// </summary>
    private static int ExtractNumber(string name)
    {
        var match = Regex.Match(name, @"\d+");
        return match.Success ? int.Parse(match.Value) : 0;
    }

    /// <summary>
    /// 獲取撲克牌牌面資料
    /// </summary>
    public PokerData GetFrontSprite(PokerStyle pokerStyle, Suit suit, Rank rank)
    {
        List<PokerData> pokerDatas = PokerDecks.Find(deck => deck.PokerStyle == pokerStyle)?.pokerDatas;
        return pokerDatas.Find(cf => cf.Suit == suit && cf.Rank == rank);
    }
}

/// <summary>
/// 撲克牌套組
/// </summary>
[System.Serializable]
public class PokerDeck
{
    public PokerStyle PokerStyle;
    public Sprite BackSprite;
    public List<PokerData> pokerDatas = new();

    // 自動生成 52 張牌資料
    public void InitializeDeck()
    {
        pokerDatas.Clear();
        foreach (Suit s in System.Enum.GetValues(typeof(Suit)))
        {
            foreach (Rank r in System.Enum.GetValues(typeof(Rank)))
            {
                pokerDatas.Add(new PokerData { Suit = s, Rank = r });
            }
        }
    }
}

/// <summary>
/// 撲克牌牌面資料
/// </summary>
[System.Serializable]
public class PokerData
{
    public Suit Suit;
    public Rank Rank;
    public Sprite FrontSprite;
}
