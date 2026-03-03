using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public class SceneLoadManager : SingletonMonoBehaviour<SceneLoadManager>
{
    protected override void OnDestroy()
    {
        base.OnDestroy();

        StopAllCoroutines();
    }

    /// <summary>
    /// 載入場景
    /// </summary>
    public void LoadScene(SceneEnum sceneEnum, Action callback = null)
    {
        // 當前場景與轉換場景一樣
        if (SceneManager.GetActiveScene().name == sceneEnum.ToString())
            return;

        StartCoroutine(ILoadScene(sceneEnum, callback));
    }

    private IEnumerator ILoadScene(SceneEnum sceneEnum, Action callback)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync((int)sceneEnum);

        while (operation != null && !operation.isDone)
        {
            yield return null;
        }

        callback?.Invoke();
    }
}
