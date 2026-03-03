using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableManager : SingletonMonoBehaviour<AddressableManager>
{
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
