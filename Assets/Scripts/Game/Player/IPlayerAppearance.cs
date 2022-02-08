﻿using RavenNest.Models;
using System;
using UnityEngine;

public interface IPlayerAppearance
{
    Gender Gender { get; }
    Transform GetCombinedMesh();
    void SetAppearance(Appearance appearance);
    void SetAppearance(SyntyAppearance appearance, Action onReady);
    bool TryUpdate(int[] values);
    int[] ToAppearanceData();
    SyntyAppearance ToSyntyAppearanceData();
    void ToggleHelmVisibility();
    void UpdateAppearance(Sprite capeLogo = null);
    void Optimize(Action afterUndo = null);
    void Equip(ItemController item);
    void Unequip(ItemController item);
    void UpdateClanCape();
    Transform MainHandTransform { get; }
    Transform OffHandTransform { get; }

    GameObject FullBodySkinMesh { get; }
    void SetFullBodySkinMesh(GameObject prefab);
    void DestroyFullBodySkinMesh();
}