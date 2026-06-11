using UnityEngine;
using UnityEngine.UI;

public class PlayerSpriteManager : IconManager
{
    void Awake()
    {
        GameManager.Instance.playerSpriteManager = this;
    }
}
