﻿using Everything_To_IMU_SlimeVR.Tracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ImuToXInput
{
    public class TrackerState
    {
        private Vector3 _position;
        private Vector3 _positionCalibration;

        private Quaternion _rotation;
        private Vector3 _euler;

        private Vector3 _eulerCalibration;


        public int TrackerId { get; set; }
        public string BodyPart { get; set; }
        public string Ip { get; set; }

        public Quaternion Rotation
        {
            get
            {
                return _rotation;
            }
            set
            {
                _rotation = value;
                _euler = _rotation.QuaternionToEuler();
            }
        }

        public Vector3 CalibratedPosition { get { return _position - _positionCalibration; } }
        public bool CloseToCalibratedY
        {
            get
            {  return CalibratedPosition.Y < 0.060f;
                return true;
            }
        }
        public Vector3 SmoothRotation { get; set; }

        public Vector3 Euler { get => _eulerCalibration - _euler; set => _euler = value; }
        public Vector3 EulerCalibration { get => _eulerCalibration; set => _eulerCalibration = value; }
        public Vector3 Position { get => _position; set => _position = value; }
        public Vector3 PositionCalibration { get => _positionCalibration; set => _positionCalibration = value; }
        public Quaternion WorldRotation { get; internal set; }
    }
}
