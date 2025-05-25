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

namespace ROS2
{
    public class Ros2PositionPublisher : MonoBehaviour
    {
        // Start is called before the first frame update
        private ROS2UnityComponent ros2Unity;
        private ROS2Node ros2Node;
        private IPublisher<trajectory_msgs.msg.JointTrajectory> joint_pub_;
        private ISubscription<trajectory_msgs.msg.JointTrajectory> joint_sub_;
        private Quaternion targetLeftRot;
        private Quaternion targetRightRot;

        private string[] joint_names_;

        [SerializeField]
        private float speed = 1.0f; // Speed of the joint movement
        [SerializeField]
        private Transform left_joint_;
        [SerializeField]
        private Transform right_joint_;

        void Awake()
        {
            ros2Unity = GetComponent<ROS2UnityComponent>();
            targetLeftRot = left_joint_.localRotation;
            targetRightRot = right_joint_.localRotation;
            joint_names_ = new string[] { "unity_joint_left", "unity_joint_right" };
        }

        void Update()
        {
            if (ros2Unity.Ok())
            {
                if (ros2Node == null)
                {
                    ros2Node = ros2Unity.CreateNode("ROS2UnityPositionNode");
                    joint_pub_ = ros2Node.CreatePublisher<trajectory_msgs.msg.JointTrajectory>("unity/state_position");
                    ros2Node = ros2Unity.CreateNode("ROS2UnityListenerNode");
                    joint_sub_ = ros2Node.CreateSubscription<trajectory_msgs.msg.JointTrajectory>(
                      "/unity/command_position", HandlePositionMessage);
                }
                trajectory_msgs.msg.JointTrajectory msg = CreatePubMsg();
                joint_pub_.Publish(msg);
            }
            left_joint_.localRotation =
            Quaternion.Slerp(left_joint_.localRotation, targetLeftRot, Time.deltaTime * speed);
            right_joint_.localRotation =
            Quaternion.Slerp(right_joint_.localRotation, targetRightRot, Time.deltaTime * speed);
        }
        
        trajectory_msgs.msg.JointTrajectory CreatePubMsg()
        {
            trajectory_msgs.msg.JointTrajectory msg = new trajectory_msgs.msg.JointTrajectory();
            // msg.Header.Stamp = ros2Node.GetClock().Now();
            // msg.JointNames = joint_names_; //TODO 何故かJointNamesがないとしてエラーになる
            trajectory_msgs.msg.JointTrajectoryPoint point = new trajectory_msgs.msg.JointTrajectoryPoint(); // LListではなく静的配列を期待している
            point.Positions = new double[] { left_joint_.localRotation.eulerAngles.y, right_joint_.localRotation.eulerAngles.y };
            point.Velocities = new double[] { 0.0, 0.0 };
            // point.TimeFromStart = ros2Node.GetClock().Now();
            var points = new trajectory_msgs.msg.JointTrajectoryPoint[] { point };
            msg.Points = points;
            return msg;
        }
        void HandlePositionMessage(trajectory_msgs.msg.JointTrajectory msg)
        {
            if (msg.Points.Length > 0)
            {
                targetLeftRot = Quaternion.AngleAxis((float)msg.Points[0].Positions[0] * Mathf.Rad2Deg, Vector3.up);;
                targetRightRot = Quaternion.AngleAxis((float)msg.Points[0].Positions[1] * Mathf.Rad2Deg, Vector3.up);; 
            }
        }
}

}  // namespace ROS2
