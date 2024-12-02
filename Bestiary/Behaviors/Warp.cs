using System.Collections.Generic;
using UnityEngine;

namespace Bestiary.Behaviors;

public class Warp : MonoBehaviour
{
    public ZNetView m_nview = null!;
    public Humanoid m_character = null!;

    public EffectList? m_startWarpEffects;
    public EffectList? m_stopWarpEffects;

    public List<string> m_startEffects = new();

    private float m_warpCooldown;
    private bool m_warping;
    public Vector3 m_warpTargetPos;
    public Quaternion m_warpTargetRot;
    public void Awake()
    {
        m_nview = GetComponent<ZNetView>();
        m_character = GetComponent<Humanoid>();
        GetWarpEffects();
    }
    
    private void GetWarpEffects()
    {
        m_startWarpEffects = new EffectList();
        m_stopWarpEffects = new EffectList();
        List<EffectList.EffectData> effects = new();
        foreach (var effectName in m_startEffects)
        {
            if (ZNetScene.instance.GetPrefab(effectName) is { } prefab)
            {
                effects.Add(new EffectList.EffectData()
                {
                    m_prefab = prefab
                });
            }
        }

        m_startWarpEffects.m_effectPrefabs = effects.ToArray();
        m_stopWarpEffects.m_effectPrefabs = effects.ToArray();
    }

    public void StartWarp()
    {
        if (!m_nview.IsOwner() || m_warpCooldown < 15.0f || m_character.InAttack()) return;
        if (Player.GetRandomPlayer() is not {} target) return;        
        WarpTo(target.transform.position - target.m_lookDir * 5f);
    }

    public void Update()
    {
        float dt = Time.fixedDeltaTime;
        UpdateWarp(dt);
    }

    public void UpdateWarp(float dt)
    {
        if (!m_warping)
        {
            m_warpCooldown += dt;
            StartWarp();
        }
        else
        {
            m_startWarpEffects?.Create(transform.position, transform.rotation, transform);
            m_warpCooldown = 0.0f;
            Vector3 dir = m_warpTargetRot * Vector3.forward;
            transform.position = m_warpTargetPos;
            transform.rotation = m_warpTargetRot;
            m_character.m_body.velocity = Vector3.zero;
            m_character.m_maxAirAltitude = transform.position.y;
            m_character.SetLookDir(dir);
            m_warping = false;
            m_stopWarpEffects?.Create(transform.position, transform.rotation, transform);
        }
    }

    public bool IsWarping() => m_warping;

    public void WarpTo(Vector3 pos)
    {
        if (IsWarping()) return;
        m_warping = true;
        m_warpCooldown = 0.0f;
        m_warpTargetPos = pos;
        m_warpTargetRot = transform.rotation;
    }
}