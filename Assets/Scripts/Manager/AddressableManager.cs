using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableManager : SingletonMonoBehaviour<AddressableManager>
{
    /// <summary>
    /// 下載預載資源
    /// </summary>
    public async void DownloadPreAssets(Action<float> progressCallback, Action finishCallback)
    {
        try
        {
            var downloadHandle = Addressables.DownloadDependenciesAsync("PreLoad");
            while (!downloadHandle.IsDone)
            {
                Debug.Log($"下載中: {downloadHandle.PercentComplete * 100}%");
                progressCallback?.Invoke(downloadHandle.PercentComplete);
                await Task.Yield();
            }
            Addressables.Release(downloadHandle);

            finishCallback?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"下載預載資源錯誤: {e}");
        }
    }

    /// <summary>
    /// 載入資源
    /// </summary>
    public async void LoadAssets(ViewEnum viewType, Action<GameObject> callback = null)
    {
        try
        {
            AsyncOperationHandle<GameObject> loadHandle = Addressables.LoadAssetAsync<GameObject>(viewType.ToString());
            await loadHandle.Task;

            GameObject prefab = loadHandle.Result;
            GameObject go = Instantiate(prefab);

            callback?.Invoke(go);
        }
        catch (Exception e)
        {
            Debug.LogError($"{viewType} 載入介面錯誤: {e}");
        }
    }
}
