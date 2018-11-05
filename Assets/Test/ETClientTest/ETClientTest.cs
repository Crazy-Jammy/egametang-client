using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Model;
using System;
using System.Net;
using System.Threading;

namespace Test
{
    public class ETClientTest : MonoBehaviour
    {
        Session gateSession;

        void Awake()
        {
            SynchronizationContext.SetSynchronizationContext(OneThreadSynchronizationContext.Instance);
            NetworkClient.Instance.Initialize(NetworkProtocol.KCP);
            NetworkOpcodeType.Instance.Initialize();
        }

        void Start()
        {
            Login();
        }

        public async void Login()
        {

            Session session = null;
            try
            {
                //IPEndPoint connetEndPoint = NetworkHelper.ToIPEndPoint();
                session = NetworkClient.Instance.Create("127.0.0.1:10002");

                R2C_Login r2CLogin = (R2C_Login) await session.Call(new C2R_Login() { Account = "Test1", Password = "111111" });
                Debug.LogError($"R2C_Login: {r2CLogin.Address}");

                if (string.IsNullOrEmpty(r2CLogin.Address))
                    return;

                //connetEndPoint = NetworkHelper.ToIPEndPoint(r2CLogin.Address);
                gateSession = NetworkClient.Instance.Create(r2CLogin.Address);
                G2C_LoginGate g2CLoginGate = (G2C_LoginGate)await gateSession.Call(new C2G_LoginGate() { Key = r2CLogin.Key });

                Debug.LogError($"登陆gate成功!{r2CLogin.Address}");
                Debug.LogError(g2CLoginGate.PlayerId.ToString());
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
            finally
            {
                session?.Dispose();
            }
        }


        void Update()
        {
            OneThreadSynchronizationContext.Instance.Update();
            NetworkClient.Instance.Update();
        }
    }
}
