using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "CardSprites", menuName = "ScriptableObjects/PokerCollection")]
public class PokerCollection : ScriptableObject
{
    public List<PokerDeck> PokerDecks;

#if UNITY_EDITOR
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
#endif

    /// <summary>
    /// 獲取撲克牌套組資料
    /// </summary>
    public PokerDeck GetFrontSprite(PokerStyle pokerStyle)
    {
        return PokerDecks.Find(deck => deck.PokerStyle == pokerStyle);
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
    public List<PokerSkinData> pokerDatas = new();

    // 自動生成 52 張牌資料
    public void InitializeDeck()
    {
        pokerDatas.Clear();
        foreach (SuitEnum s in System.Enum.GetValues(typeof(SuitEnum)))
        {
            foreach (RankEnum r in System.Enum.GetValues(typeof(RankEnum)))
            {
                SuitColorEnum sc = s == SuitEnum.Hearts || s == SuitEnum.Diamonds ? SuitColorEnum.Red : SuitColorEnum.Black;
                pokerDatas.Add(new PokerSkinData { Suit = s, Rank = r, SuitColor = sc });
            }
        }
    }
}

/// <summary>
/// 撲克牌皮膚資料
/// </summary>
[System.Serializable]
public class PokerSkinData
{
    public SuitColorEnum SuitColor;
    public SuitEnum Suit;
    public RankEnum Rank;
    public Sprite FrontSprite;
}
