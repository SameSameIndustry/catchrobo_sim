using UnityEngine;

namespace ROS2
{
    public class Battle : MonoBehaviour
    {
        private ROS2UnityComponent ros2Unity;
        private ROS2Node ros2Node;
        private ISubscription<std_msgs.msg.Empty> startMotionSub;
        private ISubscription<std_msgs.msg.Empty> resetMotionSub;
        private ISubscription<std_msgs.msg.Empty> catchMotionSub;
        private ISubscription<geometry_msgs.msg.Pose> goalPose;
        private geometry_msgs.msg.Pose latest_goal_pose_msg = null;
        private bool is_start_motion = false;
        private bool is_reset_motion = false;
        private bool is_catch_motion = false;

        private bool has_new_msg = false;

        [SerializeField]
        private GameObject _playerPrefab;
        [SerializeField]
        private bool isRedTeam = true;
        [SerializeField]
        private Vector3 initialRedPosition;
        [SerializeField]
        private Vector3 initialBluePosition;
        private GameObject _player;

        void Awake()
        {
            ros2Unity = GetComponent<ROS2UnityComponent>();
            if (isRedTeam)
            {
                _player = Instantiate(_playerPrefab, Vector3.zero, Quaternion.identity);
                _player.transform.position = initialRedPosition;
            }
            else
            {
                _player = Instantiate(_playerPrefab, Vector3.zero, Quaternion.identity);
                _player.transform.position = initialBluePosition;
            }
        }

        void Update()
        {
            if (ros2Unity.Ok())
            {
                if (ros2Node == null)
                {
                    ros2Node = ros2Unity.CreateNode("ROS2UnityPositionNode");
                    startMotionSub = ros2Node.CreateSubscription<std_msgs.msg.Empty>(
                      "/arm_move/start_motion", HandleStartMotionMessage);
                    resetMotionSub = ros2Node.CreateSubscription<std_msgs.msg.Empty>(
                      "/arm_move/reset_motion", HandleResetMotionMessage);
                    catchMotionSub = ros2Node.CreateSubscription<std_msgs.msg.Empty>(
                      "/arm_move/catch_motion", HandleCatchMotionMessage);
                    goalPose = ros2Node.CreateSubscription<geometry_msgs.msg.Pose>(
                      "/arm_move/goal_pose", HandlePoseMessage);
                }
                if (has_new_msg)
                {
                    if(is_start_motion)
                    {
                        Debug.Log("Start Motion");
                        _player.transform.position = isRedTeam ? initialRedPosition : initialBluePosition;
                        is_start_motion = false;
                    }
                    else if(is_reset_motion){
                        Debug.Log("Reset Motion");
                        _player.transform.position = isRedTeam ? initialRedPosition + new Vector3(0, 0, 1) : initialBluePosition + new Vector3(0, 0, 1);
                        is_reset_motion = false;
                    }
                    else if(is_catch_motion){
                        Debug.Log("Catch Motion");
                        is_catch_motion = false;
                    }
                    _player.transform.position = new Vector3(
                        (float)latest_goal_pose_msg.Position.X,
                        (float)latest_goal_pose_msg.Position.Y,
                        (float)latest_goal_pose_msg.Position.Z
                    );

                    has_new_msg = false;  // フラグをリセット
                }

            }
        }
        void HandlePoseMessage(geometry_msgs.msg.Pose msg)
        {
            latest_goal_pose_msg = msg;
            has_new_msg = true;
        }
        void HandleStartMotionMessage(std_msgs.msg.Empty msg)
        {
            is_start_motion = true;
            Debug.Log("Start Motion Received");
        }
        void HandleResetMotionMessage(std_msgs.msg.Empty msg)
        {
            is_reset_motion = true;
            Debug.Log("Reset Motion Received");
        }
        void HandleCatchMotionMessage(std_msgs.msg.Empty msg)
        {
            is_catch_motion = true;
            Debug.Log("Catch Motion Received");
        }
    }
}