using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    [System.Serializable]
    private class UIGroup 
    {
        public string name;
        public CanvasGroup groupParent;
        public float fadeOutDuration = 1;
        public float fadeInDuration = 1;
    }

    [SerializeField]
    private List<UIGroup> uiGroups = new List<UIGroup>();
    
    [SerializeField]
    private int defaultUIGroupIndex = 0;

    private UIGroup currentlyActive;

    public bool IsFading { get; private set; }

    private void Awake()
    {
        if (uiGroups.Count > 0 && uiGroups.Count > defaultUIGroupIndex && defaultUIGroupIndex >= 0)
        {
            currentlyActive = uiGroups[defaultUIGroupIndex];
        }
    }

    public void ActivateUI(string name) 
    {
        IsFading = true;
        FadeIn(name).Forget();
        FadeOut(currentlyActive.name).Forget();

        currentlyActive = uiGroups.Find(x => x.name == name);
    }

    public async UniTask FadeOutCurrentUI() 
    {
        if (currentlyActive != null)
        {
            await FadeOutUI(currentlyActive.name);
        }
    }

    public async UniTask FadeInCurrentUI()
    {
        if (currentlyActive != null)
        {
            await FadeInUI(currentlyActive.name);
        }
    }

    public async UniTask FadeOutUI(string name)
    {
        IsFading = true;
        await FadeOut(name);
        IsFading = false;
    }

    public async UniTask FadeInUI(string name) 
    {
        IsFading = true;
        await FadeIn(name);
    }

    private async UniTask FadeOut(string name) 
    {
        int index = uiGroups.FindIndex(x => x.name == name);

        if (index >= 0) 
        {
            var group = uiGroups[index];
            group.groupParent.alpha = 1;
            float counter = 0;

            if (group.fadeOutDuration == 0)
            {
                group.groupParent.alpha = 0;
            }
            else
            {
                while (counter <= group.fadeOutDuration)
                {
                    counter += Time.deltaTime;
                    float currentAlpha = Mathf.Lerp(1, 0, counter / group.fadeOutDuration);
                    group.groupParent.alpha = currentAlpha;

                    await UniTask.Yield(PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());
                }
            }          

            group.groupParent.gameObject.SetActive(false);
        }
    }

    private async UniTask FadeIn(string name) 
    {
        int index = uiGroups.FindIndex(x => x.name == name);

        if (index >= 0)
        {
            var group = uiGroups[index];
            group.groupParent.gameObject.SetActive(true);
            group.groupParent.alpha = 0;

            float counter = 0;

            if (group.fadeInDuration == 0)
            {
                group.groupParent.alpha = 1;
            }
            else 
            {
                while (counter <= group.fadeOutDuration)
                {
                    counter += Time.deltaTime;
                    float currentAlpha = Mathf.Lerp(0, 1, counter / group.fadeOutDuration);
                    group.groupParent.alpha = currentAlpha;

                    await UniTask.Yield(PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());
                }
            }           
        }

        IsFading = false;
    }
}
