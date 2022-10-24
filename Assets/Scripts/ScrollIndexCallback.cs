using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using LootLocker.Requests;

public class ScrollIndexCallback : MonoBehaviour 
{
    public Sprite[] bgSprites;
	public Image bgImage;
	public Text textScore, textRank, textName;

    void ScrollCellIndex (LootLockerLeaderboardMember cellData) 
    {
		textRank.text = cellData.rank.ToString();
		textName.text = cellData.member_id;
		textScore.text = cellData.score.ToString();
		bgImage.sprite = bgSprites[cellData.rank % 2];
	}
   
}
