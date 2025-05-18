// Copyright 2019-2021 Robotec.ai.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using UnityEngine;

namespace ROS2
{

/// <summary>
/// An example class provided for testing of basic ROS2 communication
/// </summary>
[RequireComponent(typeof(ROS2UnityComponent))]
public class Ros2PositionSubscriber : MonoBehaviour
{
    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    private ISubscription<std_msgs.msg.Float64> chatter_sub;
    public float speed = 1.0f; // Speed of the joint movement
    private float targetAngle;               // 目標角（rad）
    private Quaternion targetRot;            // 目標姿勢

        void Awake()
        {
            ros2Unity = GetComponent<ROS2UnityComponent>();
            targetRot = transform.localRotation;
    }

    void Update()
    {
        if (ros2Node == null && ros2Unity.Ok())
        {
            ros2Node = ros2Unity.CreateNode("ROS2UnityListenerNode");
            chatter_sub = ros2Node.CreateSubscription<std_msgs.msg.Float64>(
              "/unity/command_position", HandlePositionMessage);
        }
        transform.localRotation =
            Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * speed);
    }
    void HandlePositionMessage(std_msgs.msg.Float64 msg)
    {
        targetAngle = (float)msg.Data;
        targetRot = Quaternion.AngleAxis(targetAngle * Mathf.Rad2Deg, Vector3.up);
    }
}

}  // namespace ROS2
