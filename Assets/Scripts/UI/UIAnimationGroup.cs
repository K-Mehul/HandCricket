using UnityEngine;
using System.Linq;

public class UIAnimationGroup : MonoBehaviour
{
    public float staggerDelay = 0.1f;
    public bool findChildrenOnEnable = true;

    private UIAnimation[] animations;

    private void OnEnable()
    {
        if (findChildrenOnEnable)
        {
            PlayGroup();
        }
    }

    [ContextMenu("Play Group")]
    public void PlayGroup()
    {
        animations = GetComponentsInChildren<UIAnimation>()
            .OrderBy(a => a.transform.GetSiblingIndex())
            .ToArray();

        for (int i = 0; i < animations.Length; i++)
        {
            animations[i].delay = i * staggerDelay;
            animations[i].Play();
        }
    }
}
