using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public abstract class NetworkAddressableMonoBehavior : MonoBehaviourPunCallbacks, INetworkAddressable
{
    [SerializeField] bool isEnqueued;
    public bool IsEnqueued { get => isEnqueued; set => isEnqueued = value; }

    [SerializeField] PhotonView photonView;
    public PhotonView PhotonView { get => photonView; }
    public int PhotonViewId { get => photonView.ViewID; }

    public event Action<INetworkAddressable> SetParentToObjectPoolAct;
    public event Action<INetworkAddressable, bool> OnAssetEnabledAct;
    public event Action<INetworkAddressable, bool> OnAssetDisabledAct;
    public event Action<INetworkAddressable, bool> OnAssetDestroyedAct;

    public event Action OnAssetDisabledAct_Init;

    public override void OnEnable()
    {
        OnAssetEnabledAct?.Invoke(this, NetworkManager.Instance.IsMasterClient);
    }

    public override void OnDisable()
    {
        OnAssetDisabledAct?.Invoke(this, NetworkManager.Instance.IsMasterClient);
        OnAssetDisabledAct_Init?.Invoke();
        OnAssetDisabledAct_Init = null;
    }

    protected virtual void OnDestroy()
    {
        OnAssetDestroyedAct?.Invoke(this, NetworkManager.Instance.IsMasterClient);
    }

    public void SetParentToObjectPool()
    {
        SetParentToObjectPoolAct.Invoke(this);
    }

    protected void SetNetworkDeactive()
    {
        SetNetworkActive(false, transform.position);
    }

    public void SetNetworkActive(bool value, Vector3 position)
    {
        photonView.RPC(nameof(SetActive_RPC), RpcTarget.AllBuffered, value, position);
    }

    [PunRPC]
    protected void SetActive_RPC(bool value, Vector3 position)
    {
        gameObject.SetActive(value);

        if (value)
            gameObject.transform.position = position;
    }
}
