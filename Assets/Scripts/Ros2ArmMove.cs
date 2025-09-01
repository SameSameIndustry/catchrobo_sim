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
        private IPublisher<trajectory_msgs.msg.JointTrajectory> joint_pub_;
        private ISubscription<trajectory_msgs.msg.JointTrajectory> joint_sub_;
        private ISubscription<geometry_msgs.msg.Pose> _goalPose;

        private trajectory_msgs.msg.JointTrajectory latest_msg = null;
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
                    joint_pub_ = ros2Node.CreatePublisher<trajectory_msgs.msg.JointTrajectory>("unity/state_position");
                    joint_sub_ = ros2Node.CreateSubscription<trajectory_msgs.msg.JointTrajectory>(
                      "/unity/command_position", HandlePositionMessage);
                    _goalPose = ros2Node.CreateSubscription<geometry_msgs.msg.Pose>(
                     "/arm_move/goal_pose", HandlePoseMessage);
                }
                trajectory_msgs.msg.JointTrajectory msg = CreatePubMsg();
                joint_pub_.Publish(msg);
                if (has_new_msg && latest_msg != null)
                {
                    for (int i = 0; i < latest_msg.Points[0].Positions.Length; i++)
                    {
                        var body = articulationBodies[i];
                        var drive = body.xDrive;
                        drive.target = Mathf.Rad2Deg * (float)latest_msg.Points[0].Positions[i];
                        Debug.Log($"Joint {i} target angle: {drive.target} degrees");
                        body.xDrive = drive;
                    }
                    has_new_msg = false;  // フラグをリセット
                }
                for (int i = 0; i < articulationBodies.Length; i++)
                {
                    var body = articulationBodies[i];
                    var drive = body.xDrive;
                    Debug.Log($"Joint {i} target angle: {drive.target} degrees");
                }
            }
        }

        trajectory_msgs.msg.JointTrajectory CreatePubMsg()
        {
            trajectory_msgs.msg.JointTrajectory msg = new trajectory_msgs.msg.JointTrajectory();
            // msg.Header.Stamp = ros2Node.GetClock().Now();
            msg.Joint_names = joint_names_; //TODO 何故かJointNamesがないとしてエラーになる
            trajectory_msgs.msg.JointTrajectoryPoint point = new trajectory_msgs.msg.JointTrajectoryPoint(); // LListではなく静的配列を期待している

            List<double> positions = new List<double>();
            for (int i = 0; i < articulationBodies.Length; i++)
            {
                // Get the current rotation of the joint in radians
                float currentAngle = articulationBodies[i].jointPosition[0];
                positions.Add(currentAngle);
            }
            point.Positions = positions.ToArray();

            point.Velocities = new double[] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 }; // Set velocities to zero for now
            // point.TimeFromStart = ros2Node.GetClock().Now();
            var points = new trajectory_msgs.msg.JointTrajectoryPoint[] { point };
            msg.Points = points;
            return msg;
        }
        void HandlePositionMessage(trajectory_msgs.msg.JointTrajectory msg)
        {
            latest_msg = msg;
            has_new_msg = true;
        }
        // 自分で肘のところを曲げるようにする
        void HandlePoseMessage(geometry_msgs.msg.Pose msg)
        {
            // Handle the incoming pose message
            float x = (float)msg.Position.X;
            float y = (float)msg.Position.Y;
            float z = (float)msg.Position.Z;
            float r = Mathf.Sqrt(x * x + y * y);
            float currentLeftRadialRotationRads = articulationBodies[0].jointPosition[0];
            var elbow_angle = Mathf.Asin((r - l2 * Mathf.Sin(currentLeftRadialRotationRads)) / l3);
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

}  // namespace ROS2
