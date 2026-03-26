using System;
using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
	[ComponentOf(typeof(Scene))]
	public class OperaComponent: Entity, IAwake, IUpdate
    {
        public Vector3 ClickPoint;

	    public int mapMask;

        public float3 LastDirection;    // cached last sent direction for change detection
        public bool IsDirectMoving;     // true when WASD movement is active
    }
}
