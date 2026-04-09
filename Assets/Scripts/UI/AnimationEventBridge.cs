using UnityEngine;

public class AnimationEventBridge : MonoBehaviour
{
    [Tooltip("Reference to the main GameView script in the scene")]
    public GameView gameView;

    /// <summary>
    /// Called via Unity Animation Event on the Bowler's clip
    /// </summary>
    public void OnReleaseBall()
    {
        if (gameView != null)
        {
            gameView.OnReleaseBall();
        }
    }

    /// <summary>
    /// Called via Unity Animation Event on the Batsman's shot clips
    /// </summary>
    public void OnBatsmanContact()
    {
        if (gameView != null)
        {
            gameView.OnBatsmanContact();
        }
    }
}
