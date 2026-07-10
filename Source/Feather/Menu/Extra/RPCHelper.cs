using ExitGames.Client.Photon;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Feather.Menu.Extra
{
    public class RPCHelper : MonoBehaviour
    {
        public static void AntiRPCKick()
        {
            try
            {
                AntiRPCKicker();
                Type gorillaNotType = typeof(MonkeAgent);
                MonkeAgent gorillaInstance = MonkeAgent.instance;
                if (gorillaInstance == null)
                    return;
                MonkeAgent.instance.rpcErrorMax = int.MaxValue;
                MonkeAgent.instance.rpcCallLimit = int.MaxValue;
                MonkeAgent.instance.logErrorMax = int.MaxValue;
                PhotonNetwork.MaxResendsBeforeDisconnect = int.MaxValue;
                PhotonNetwork.QuickResends = int.MaxValue;
                var peer = PhotonNetwork.NetworkingClient?.LoadBalancingPeer;
                if (peer != null)
                {
                    peer.SentCountAllowance = int.MaxValue;
                    peer.QuickResendAttempts = 3;
                    peer.CrcEnabled = false;
                    peer.UseByteArraySlicePoolForEvents = false;
                    peer.TrafficStatsEnabled = false;
                    peer.TrafficStatsReset();
                    peer.SendOutgoingCommands();
                    try
                    {
                        var type = peer.GetType();
                        var queueField = type.GetField("outgoingStreamQueue", BindingFlags.NonPublic | BindingFlags.Instance);
                        var queue = queueField?.GetValue(peer) as System.Collections.IList;
                        queue?.Clear();
                        var commandsField = type.GetField("commandList", BindingFlags.NonPublic | BindingFlags.Instance);
                        var commands = commandsField?.GetValue(peer) as System.Collections.IList;
                        commands?.Clear();
                        var resentField = type.GetField("resentCommandsCount", BindingFlags.NonPublic | BindingFlags.Instance);
                        resentField?.SetValue(peer, 0);
                    }
                    catch { }
                }
                PhotonNetwork.NetworkStatisticsEnabled = false;
                ValueTuple<Type, object, string, bool>[] targets = new ValueTuple<Type, object, string, bool>[]
                {
            (gorillaNotType, gorillaInstance, "rpcErrorMax", false),
            (gorillaNotType, gorillaInstance, "rpcCallLimit", false),
            (gorillaNotType, gorillaInstance, "logErrorMax", false),
            (gorillaNotType, gorillaInstance, "userRPCCalls", false),
            (gorillaNotType, gorillaInstance, "_sendReport", false),
            (typeof(PhotonNetwork), null, "QuickResends", true),
            (typeof(PhotonNetwork), null, "MaxResendsBeforeDisconnect", true)
                };
                foreach (var entry in targets)
                    TrySetMember(entry.Item1, entry.Item2, entry.Item3, GetDefaultValue(entry.Item3), entry.Item4);
                try
                {
                    var userRPCCallsField = gorillaNotType.GetField("userRPCCalls", BindingFlags.NonPublic | BindingFlags.Instance);
                    var userRPCCalls = userRPCCallsField?.GetValue(gorillaInstance) as System.Collections.IDictionary;
                    userRPCCalls?.Clear();
                }
                catch { }
                PhotonNetwork.NetworkingClient.OpRaiseEvent(200, new System.Collections.Hashtable()
        {
            { 0, GorillaTagger.Instance.myVRRig.ViewID }
        }, new RaiseEventOptions
        {
            CachingOption = (EventCaching)6,
            TargetActors = new int[] { PhotonNetwork.LocalPlayer.ActorNumber }
        }, SendOptions.SendReliable);
                if (Time.time > rpcDel)
                {
                    try
                    {
                        rpcDel = Time.time + 0.47f;
                        PhotonNetwork.RemoveBufferedRPCs(int.MaxValue, null, null);
                        PhotonNetwork.RemoveRPCs(PhotonNetwork.LocalPlayer);
                        PhotonNetwork.OpCleanActorRpcBuffer(PhotonNetwork.LocalPlayer.ActorNumber);
                        PhotonNetwork.OpCleanRpcBuffer(GorillaTagger.Instance.myVRRig.GetView);
                        PhotonNetwork.NetworkingClient.LoadBalancingPeer.SendOutgoingCommands();
                        Traverse yeah = Traverse.Create(typeof(PhotonNetwork));
                        yeah.Property("ResentReliableCommands").SetValue(0);
                        PhotonNetwork.NetworkingClient.Service();
                        PhotonNetwork.NetworkingClient.OpChangeGroups(null, new byte[] { 1, 2, 3, 4 });
                        PhotonNetwork.NetworkingClient.LoadBalancingPeer.TrafficStatsReset();
                        try
                        {
                            var system = AppDomain.CurrentDomain.GetAssemblies()
                                .First(a => a.GetName().Name == "Assembly-CSharp")
                                .GetType("RoomSystem");

                            system.GetMethod("OnPlayerLeftRoom", BindingFlags.NonPublic | BindingFlags.Instance)
                                .Invoke(null, new object[] { NetworkSystem.Instance.LocalPlayer });
                        }
                        catch { }
                        try
                        {
                            NetSystemState state = new NetSystemState();
                            PeerStateValue val = new PeerStateValue();
                            state.Equals(NetSystemState.Connecting);
                            val.Equals(PeerStateValue.Connected);
                            RunViewUpdate();
                        }
                        catch { }
                        PhotonNetwork.SendAllOutgoingCommands();
                        try
                        {
                            var photonViewList = typeof(PhotonNetwork).GetField("photonViewList",
                                BindingFlags.NonPublic | BindingFlags.Static);
                            var viewDict = photonViewList?.GetValue(null) as System.Collections.IDictionary;
                            if (viewDict != null)
                            {
                                var keysToRemove = new System.Collections.ArrayList();
                                foreach (System.Collections.DictionaryEntry entry in viewDict)
                                {
                                    var view = entry.Value as PhotonView;
                                    if (view != null && view.IsMine && view.isRuntimeInstantiated)
                                        keysToRemove.Add(entry.Key);
                                }
                                foreach (var key in keysToRemove)
                                    viewDict.Remove(key);
                            }
                        }
                        catch { }
                    }
                    catch { }
                }
                MethodInfo refresh = gorillaNotType.GetMethod("RefreshRPCs", BindingFlags.NonPublic | BindingFlags.Instance);
                refresh?.Invoke(gorillaInstance, null);
            }
            catch { }
        }
        private static byte[] cachedSerializedRpc;
        private static float rpcDel;

        private static void AntiRPCKicker()
        {
            for (int i = 0; i < 1200; i++)
                ResendCachedRpc();
            try
            {
                var peer = PhotonNetwork.NetworkingClient.LoadBalancingPeer;
                var field = peer.GetType().GetField("outgoingStreamQueue", BindingFlags.Instance | BindingFlags.NonPublic);

                if (field != null)
                {
                    IList list = field.GetValue(peer) as IList;
                    if (list != null && list.Count > 0)
                        cachedSerializedRpc = list[list.Count - 1] as byte[];
                }
            }
            catch
            {
                cachedSerializedRpc = null;
            }
        }

        private static void ResendCachedRpc()
        {
            if (cachedSerializedRpc == null)
                return;
            try
            {
                var peer = PhotonNetwork.NetworkingClient.LoadBalancingPeer;
                var type = peer.GetType();
                var method = type.GetMethod("SendReliable", BindingFlags.Instance | BindingFlags.NonPublic)
                            ?? type.GetMethod("SendUnreliable", BindingFlags.Instance | BindingFlags.NonPublic);
                method?.Invoke(peer, new object[] { cachedSerializedRpc });
            }
            catch
            {
                SetTick(9999f);
            }
        }

        public static void SetTick(float tickMultiplier)
        {
            var photonMono = GameObject.Find("PhotonMono")?.GetComponent<PhotonHandler>();
            if (photonMono != null)
            {
                Traverse.Create(photonMono).Field("nextSendTickCountOnSerialize").SetValue((int)(Time.realtimeSinceStartup * tickMultiplier));
                PhotonHandler.SendAsap = true;
            }
        }

        private static bool TrySetMember(Type type, object instance, string fieldName, object value, bool isStatic)
        {
            try
            {
                var field = type.GetField(fieldName,
                    (isStatic ? BindingFlags.Static : BindingFlags.Instance) |
                    BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(instance, value);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private static object GetDefaultValue(string fieldName)
        {
            if (fieldName.Contains("Max") || fieldName.Contains("Limit") || fieldName.Contains("Count"))
                return int.MaxValue;
            if (fieldName.Contains("userRPCCalls"))
                return null;
            if (fieldName.Contains("_sendReport"))
                return false;
            return null;
        }

        public static void RPCProtection()
        {
            AntiRPCKick();
        }

        public static object RunViewUpdate()
        {
            return typeof(PhotonNetwork).GetMethod("RunViewUpdate", BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, null);
        }
    }
}
