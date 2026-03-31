using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;

public class UGS_Initializer : MonoBehaviour
{
    async void Start()
    {
        DontDestroyOnLoad(gameObject);
        await InitializeUGS();
    }

    private async Task InitializeUGS()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                // Sử dụng hàm đăng nhập ẩn danh mặc định
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                Debug.Log("UGS: Đăng nhập ẩn danh thành công! PlayerID: " + AuthenticationService.Instance.PlayerId);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("UGS Error: " + e.Message);
        }
    }
}