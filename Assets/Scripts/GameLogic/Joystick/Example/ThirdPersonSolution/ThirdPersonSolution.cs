﻿// Copyright (c) Bian Shanghai
// https://github.com/Bian-Sh/UniJoystick
// Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace zFrame.Example
{
    using UnityEngine;
    using zFrame.UI;
    public class ThirdPersonSolution : MonoBehaviour
    {
        [SerializeField] Joystick joystick;
        public float speed = 2;
        CharacterController controller;
        Animation animotion;
        void Start()
        {
            controller = GetComponent<CharacterController>();
            animotion = GetComponent<Animation>();

            joystick.OnValueChanged.AddListener(v =>
            {
                if (v.magnitude != 0)
                {
                    Vector3 direction = new Vector3(v.x, 0, v.y);
                    controller.Move(direction * speed * Time.deltaTime);
                    transform.rotation = Quaternion.LookRotation(new Vector3(v.x, 0, v.y));
                   // Camera.main.transform.parent = transform;
                    animotion.Play("runSword");
                }
            });
        }
    }
}
