using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public static Action OnSceneLoaded;
    public static bool IsLoading = false;

    public static void LoadScene(string sceneName, bool additive, bool useFade = true) 
    {
        LoadSceneAsync(sceneName, additive, useFade).Forget();
    }

    private static async UniTask LoadSceneAsync(string sceneName, bool additive, bool useFade) 
    {
        if (useFade)
        {
            SingletonInterface.SingletonLocator.GameUIManager.ActivateUI("Loading");

            await UniTask.DelayFrame(1);
            await UniTask.WaitUntil(() => { return SingletonInterface.SingletonLocator.GameUIManager.IsFading == false; });
        }

        SceneInstance scene = await Addressables.LoadSceneAsync(sceneName, activateOnLoad: false, loadMode: additive ? LoadSceneMode.Additive : LoadSceneMode.Single);
        await scene.ActivateAsync();

        await UniTask.WaitForSeconds(0.5f);

        OnSceneLoaded?.Invoke();
    }
}
