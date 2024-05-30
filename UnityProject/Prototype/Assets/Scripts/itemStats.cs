using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu]

public class itemStats : ScriptableObject
{
    [Header("Weapons/otherItems")]
    public GameObject itemModel;
    public AudioClip itemSound;
    [Range(0, 1)] public float vol;
    public Image icon;
    public ParticleSystem hitEffect;

    [Header("Weapon")]
    [Range(1,10)] public int weaponDmg;
    [Range(1,500)]public int weaponDistance;
    [Range(.01f,3)] public float weaponSpeed;
    public int ammoCur;
    public int ammoMax;
   
}