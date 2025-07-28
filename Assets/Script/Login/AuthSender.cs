using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Threading.Tasks;

// �α��� �� ȸ������ ���� ������ �޽��� �۽� Ŭ����
public static class AuthSender
{
    // �α��� ��û
    public static async Task SendLoginRequest(string id, string pw)
    {
        string message = $"LOGIN|{id},{pw}\n";
        await SendToServer(message);
        
    }

    // ȸ������ ��û
    public static async Task SendRegisterRequest(string id, string pw, string nickname)
    {
        string message = $"REGISTER|{id},{pw},{nickname}\n";
        await SendToServer(message);

    }

    // ������ �޽��� ������ �Լ�
    public static async Task SendToServer(string message)
    {
        try
        {
            var stream = NetworkConnector.Instance.Stream;
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            await Task.Delay(100);
            await stream.WriteAsync(bytes, 0, bytes.Length);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("���� �޽��� ���� ����: " + ex.Message);
        }
    }

}
