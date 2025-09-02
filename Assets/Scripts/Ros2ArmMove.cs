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

using UnityEngine;
using System.Collections.Generic;

namespace ROS2
{
    public class Ros2ArmMove : MonoBehaviour
    {
        // Start is called before the first frame update
        private ROS2UnityComponent ros2Unity;
        private ROS2Node ros2Node;
        private IPublisher<sensor_msgs.msg.JointState> joint_pub_;
        private ISubscription<sensor_msgs.msg.JointState> joint_sub_;

        private sensor_msgs.msg.JointState latest_msg = null;

        private bool has_new_msg = false;

        [SerializeField]
        private string[] joint_names_;
        public ArticulationBody[] articulationBodies;
        [SerializeField]
        float l1 = 0.4f;
        [SerializeField]
        float l2 = 0.4f;
        [SerializeField]
        float l3 = 0.4f;
        [SerializeField]
        float initial_left_radial_angle = 1.57f;
        [SerializeField]
        float initial_elbow_angle = 0.502f;
        [SerializeField]
        ArticulationBody _leftElbow;
        [SerializeField]
        ArticulationBody _rightElbow;

        [SerializeField]
        private float speed = 20.0f; // Speed of the joint movement
        private List<ArticulationDrive> aDrive;

        void Awake()
        {
            ros2Unity = GetComponent<ROS2UnityComponent>();
            aDrive = new List<ArticulationDrive>();

        }

        void Update()
        {
            if (ros2Unity.Ok())
            {
                if (ros2Node == null)
                {
                    ros2Node = ros2Unity.CreateNode("ROS2UnityPositionNode");
                    joint_pub_ = ros2Node.CreatePublisher<sensor_msgs.msg.JointState>("unity/state_position");
                    joint_sub_ = ros2Node.CreateSubscription<sensor_msgs.msg.JointState>(
                      "/unity/command_position", HandlePositionMessage);

                }
                sensor_msgs.msg.JointState msg = CreatePubMsg();
                joint_pub_.Publish(msg);
                if (has_new_msg && latest_msg != null)
                {
                    for (int i = 0; i < latest_msg.Position.Length; i++)
                    {
                        var body = articulationBodies[i];
                        var drive = body.xDrive;
                        drive.target = Mathf.Rad2Deg * (float)latest_msg.Position[i];
                        body.xDrive = drive;
                    }
                    has_new_msg = false;  // フラグをリセット
                }
                // 肘の角度を決定
                var elbow_angle = DecideElbowAngle();
                var leftBody = _leftElbow;
                var rightBody = _rightElbow;
                var leftDrive = leftBody.xDrive;
                leftDrive.target = Mathf.Rad2Deg * elbow_angle;
                leftBody.xDrive = leftDrive;
                var rightDrive = rightBody.xDrive;
                rightDrive.target = -Mathf.Rad2Deg * elbow_angle;
                rightBody.xDrive = rightDrive;

            }
        }

        sensor_msgs.msg.JointState CreatePubMsg()
        {
            sensor_msgs.msg.JointState msg = new sensor_msgs.msg.JointState(); // LListではなく静的配列を期待している
            // msg.Header.Stamp = ros2Node.GetClock().Now();
            msg.Name = joint_names_;

            List<double> positions = new List<double>();
            for (int i = 0; i < articulationBodies.Length; i++)
            {
                // Get the current rotation of the joint in radians
                float currentAngle = articulationBodies[i].jointPosition[0];
                positions.Add(currentAngle);
            }
            msg.Position = positions.ToArray();

            msg.Velocity = new double[] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 }; // Set velocities to zero for now

            return msg;
        }
        void HandlePositionMessage(sensor_msgs.msg.JointState msg)
        {
            latest_msg = msg;
            has_new_msg = true;
        }

        float DecideElbowAngle()
        {
            float currentLeftRadialRotationRads = articulationBodies[0].jointPosition[0] + initial_left_radial_angle; // 1つ目の関節(left_radialを期待)の現在の回転角度をラジアンで取得
            var elbow_angle = Mathf.Acos((l1/2 - l2 * Mathf.Cos(currentLeftRadialRotationRads)) / l3);
            return elbow_angle - initial_elbow_angle;
        }
}

}  // namespace ROS2
