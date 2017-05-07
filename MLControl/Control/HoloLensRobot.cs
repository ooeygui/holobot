/*
    Copyright(c) Microsoft Corp. All rights reserved.
    
    The MIT License(MIT)
    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files(the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions :
    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
*/

using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.ComponentModel;
using System.Linq;

namespace Control
{
    class HoloLensRobot : Bindable
    {
        public const float wheelDiameter = 16.0f; // CM
        public const float wheelCircumference = (float)(Math.PI * wheelDiameter); // CM
        public const byte microStep = 1;
        public const short stepsPerRotation = (200 * microStep);
        public const float downGear = 18.0f/90.0f;
        public const float stepsPerCM = stepsPerRotation / wheelCircumference / downGear;
        public const float wheelBase = 38.1f; // CM
        public const float wheelBaseRadius = wheelBase / 2.0f;
        public const short maxSpeed = 1500;
        public const short acceleration = 800;


        private ArduinoComPort arduinoPort = new ArduinoComPort();

        private byte stepperLeftDevice = 0;
        private byte stepperRightDevice = 1;

        private byte stepperLeftEnable = 5;     // Inverted enable
        private byte stepperRightEnable = 8;     // Inverted enable

        private float stepperLeftProgress = 0;
        private float stepperRightProgress = 0;

        private int outstandingMovesLeft = 0;
        private int outstandingMovesRight = 0;

        private uint runDistance = (uint)(100 * stepsPerCM); //cm
        private uint rampDistance = (uint)(5 * stepsPerCM); //cm
        private double previousLeftThrottle = 0.0;
        private double previousRightThrottle = 0.0;

        private byte rightDirection = 1;
        private byte leftDirection = 1;

        /// mission is the image we are looking for.
        CancellationTokenSource cancellationTokenSource;


        private double lowEndCutOff = .2;

        public CameraHandler handler;

        int currentMission = -1;
        float heading = 0.0f;

        public int CurrentMission
        {
            get
            {
                return currentMission;
            }

            internal set
            {
                currentMission = value;
                NotifyPropertyChanged("CurrentMission");
            }
        }

        public float Heading
        {
            get
            {
                return heading;
            }

            internal set
            {
                heading = value;
                NotifyPropertyChanged("Heading");
            }
        }


        public bool HasArduino
        {
            get { return arduinoPort.IsConnected;  }
            private set { }
        }

        public float StepperLeftProgress
        {
            get { return stepperLeftProgress; }
            private set
            {
                stepperLeftProgress = value;
                NotifyPropertyChanged("StepperLeftProgress");
                NotifyPropertyChanged("MoveProgress");
            }
        }
        public float StepperRightProgress
        {
            get { return stepperRightProgress; }
            private set
            {
                stepperRightProgress = value;
                NotifyPropertyChanged("StepperRightProgress");
                NotifyPropertyChanged("MoveProgress");
            }
        }
        public float MoveProgress
        {
            get
            {
                float progress = StepperLeftProgress * StepperRightProgress;
                return stepperRightProgress;
            }
            private set
            {
            }
        }

        public bool IsMoving
        {
            get { return outstandingMovesLeft > 0 || outstandingMovesRight > 0;  }
            private set { }

        }

        public async Task ConnectToArduino()
        {
            await arduinoPort.GetDevice();

            await arduinoPort.SendStepperConfig(stepperLeftDevice, stepsPerRotation, 2, 4);
            await arduinoPort.SendStepperConfig(stepperRightDevice, stepsPerRotation, 6, 7);

            await arduinoPort.SetPinMode(stepperLeftEnable, ArduinoComPort.PinMode.Output);
            await arduinoPort.DigitalWrite(stepperLeftEnable, ArduinoComPort.PinState.High);

            await arduinoPort.SetPinMode(stepperRightEnable, ArduinoComPort.PinMode.Output);
            await arduinoPort.DigitalWrite(stepperRightEnable, ArduinoComPort.PinState.High);

        }



        private float arcLength(float deg, float radius)
        {
            return (float)((Math.PI * radius * deg) / 180.0f);
        }

        public async Task Move(float distance, CancellationTokenSource cancelation = null)
        {
            await Move(distance, distance, cancelation);
        }

        public async Task MoveAnalog(double xAnalog, double yAnalog)
        {
            // Convert from analog positions to left/right distances

            if (Math.Abs(xAnalog) < lowEndCutOff)
            {
                xAnalog = 0.0;
            }

            if (Math.Abs(yAnalog) < lowEndCutOff)
            {
                yAnalog = 0.0;
            }

            double leftMotorThrottle = yAnalog + xAnalog;
            double rightMotorThrottle = yAnalog - xAnalog;

            if (leftMotorThrottle > 1.0)
            {
                leftMotorThrottle = 1.0;
            }

            if (rightMotorThrottle > 1.0)
            {
                rightMotorThrottle = 1.0;
            }

            Debug.WriteLine("Throttle: " + leftMotorThrottle + "x" + rightMotorThrottle);

            outstandingMovesRight = 1;
            outstandingMovesLeft = 1;
            short leftSpeed;

            uint leftRunDistance = runDistance;
            uint rightRunDistance = runDistance;

            if (leftMotorThrottle < 0)
            {
                leftDirection = 0;
            }


            if (Math.Abs(leftMotorThrottle) < lowEndCutOff)
            {
                if (previousLeftThrottle < 0.0)
                {
                    leftDirection = 0;
                }

                leftSpeed = (short)(Math.Abs(previousLeftThrottle) * maxSpeed);
                leftRunDistance = rampDistance;
                // Ramp to zero.
            }
            else
            {
                leftSpeed = (short)(Math.Abs(leftMotorThrottle) * maxSpeed);
            }

            if (Math.Abs(previousLeftThrottle) < lowEndCutOff)
            {
                // Letting it coast to zero
                Debug.WriteLine("Ramping left to zero");
            }
            else
            {

                await arduinoPort.DigitalWrite(stepperLeftEnable, ArduinoComPort.PinState.Low);
                await arduinoPort.SendStepperStep(stepperLeftDevice, leftDirection, leftRunDistance, leftSpeed, acceleration,
                    async () =>
                    {
                        outstandingMovesLeft--;
                        await arduinoPort.DigitalWrite(stepperLeftEnable, ArduinoComPort.PinState.High);
                    },
                    (float progress) =>
                    {
                        StepperLeftProgress = progress;
                    });
            }

            short rightSpeed;
            if (rightMotorThrottle < 0)
            {
                rightDirection = 0;
            }

            if (Math.Abs(rightMotorThrottle) < lowEndCutOff)
            {
                if (previousRightThrottle < 0.0)
                {
                    rightDirection = 0;
                }

                rightSpeed = (short)(Math.Abs(previousRightThrottle) * maxSpeed);
                rightRunDistance = rampDistance;

                // Ramp to zero.
            }
            else
            {
                rightSpeed = (short)(Math.Abs(rightMotorThrottle) * maxSpeed);
            }

            if (Math.Abs(previousRightThrottle) < lowEndCutOff)
            {
                // Letting it coast to zero
                Debug.WriteLine("Ramping right to zero");
            }
            else
            {
                await arduinoPort.DigitalWrite(stepperRightEnable, ArduinoComPort.PinState.Low);
                await arduinoPort.SendStepperStep(stepperRightDevice, rightDirection, rightRunDistance, rightSpeed, acceleration,
                    async () =>
                    {
                        outstandingMovesRight--;
                        await arduinoPort.DigitalWrite(stepperRightEnable, ArduinoComPort.PinState.High);
                    },
                    (float progress) =>
                    {
                        StepperRightProgress = progress;
                    });
            }

            previousLeftThrottle = leftMotorThrottle;
            previousRightThrottle = rightMotorThrottle;
        }

        public Task<bool> Move(float rightDistance, float leftDistance, CancellationTokenSource cancelation = null)
        {
            var completion = new TaskCompletionSource<bool>();
            if (cancelation != null && cancelation.IsCancellationRequested)
            {
                completion.SetResult(false);
                return completion.Task;
            }

            var ignore = Task.Run(async () =>
            {

                rightDirection = (byte)((rightDistance > 0)? 1 : 0);
                rightDistance = Math.Abs(rightDistance);
                leftDirection = (byte)((leftDistance > 0) ? 1 : 0); ;
                leftDistance = Math.Abs(leftDistance);

                var distanceL = (uint)Math.Floor(stepsPerCM * leftDistance);
                var distanceR = (uint)Math.Floor(stepsPerCM * rightDistance);

                // We can now overlap moves; however, last command wins, but we need to complete all outstanding ones
                Interlocked.Increment(ref outstandingMovesLeft);
                Interlocked.Increment(ref outstandingMovesRight);

                await arduinoPort.DigitalWrite(stepperLeftEnable, ArduinoComPort.PinState.Low);
                await arduinoPort.DigitalWrite(stepperRightEnable, ArduinoComPort.PinState.Low);

                await arduinoPort.SendStepperStep(stepperLeftDevice, leftDirection, distanceL, maxSpeed, acceleration,
                    async () =>
                    {
                        Interlocked.Decrement(ref outstandingMovesLeft);
                        await arduinoPort.DigitalWrite(stepperLeftEnable, ArduinoComPort.PinState.High);
                        if (outstandingMovesLeft == 0 &&
                            outstandingMovesRight == 0)
                        {
                            completion.SetResult(true);
                        }
                    },
                    (float progress) =>
                    {
                        StepperLeftProgress = progress;
                    });

                await arduinoPort.SendStepperStep(stepperRightDevice, rightDirection, distanceR, maxSpeed, acceleration,
                    async () =>
                    {
                        Interlocked.Decrement(ref outstandingMovesRight);
                        await arduinoPort.DigitalWrite(stepperRightEnable, ArduinoComPort.PinState.High);

                        if (outstandingMovesLeft == 0 &&
                            outstandingMovesRight == 0)
                        {
                            completion.SetResult(true);
                        }
                    },
                    (float progress) =>
                    {
                        StepperRightProgress = progress;
                    });

            });

            return completion.Task;
        }

        public async Task Rotate(float degrees, CancellationTokenSource cancelation = null)
        {
            var lengthInCM = arcLength(degrees, wheelBaseRadius);
            await Move(-lengthInCM, lengthInCM, cancelation);
        }

        public async Task Stop()
        {
            if (outstandingMovesLeft == 0 &&
                outstandingMovesRight == 0)
            {
                // we are stopped.
                return;
            }

            float leftDist = 0;
            float rightDist = 0;
            if (outstandingMovesLeft > 0)
            {
                leftDist = leftDirection > 0? rampDistance : -rampDistance;
            }
            if (outstandingMovesRight > 0)
            {
                rightDist = rightDirection > 0 ? rampDistance : -rampDistance; ;
            }

            await Move(leftDist, rightDist, null);
        }

        public async Task Mission(int mission)
        {
            if (CurrentMission == -1)
            {
                CurrentMission = mission;
                handler.PropertyChanged += Handler_PropertyChanged;

                cancellationTokenSource = new CancellationTokenSource();
                Heading = 0;


                foreach (var i in Enumerable.Range(0, 5))
                {
                    await Rotate(90, cancellationTokenSource);       // Right
                    Heading = 90;
                    await Move(150, cancellationTokenSource);        // Forward 1.5m
                    await Rotate(-90, cancellationTokenSource);      // Left
                    Heading = 0;
                    await Move(20, cancellationTokenSource);         // Forward 20cm
                    await Rotate(-90, cancellationTokenSource);      // Left
                    Heading = -90;

                    await Move(150, cancellationTokenSource);        // Forward  1.5m
                    await Rotate(90, cancellationTokenSource);      // Right
                    await Move(20, cancellationTokenSource);         // Forward 20cm
                    Heading = 0;
                }

                CurrentMission = -1;
            }

        }

        private async void Handler_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.CompareOrdinal(e.PropertyName, "ClassifiedImage") == 0)
            {
                if (handler.ClassifiedImage == CurrentMission)
                {
                    handler.PropertyChanged -= Handler_PropertyChanged;
                    if (cancellationTokenSource != null)
                    {
                        cancellationTokenSource.Cancel();
                    }

                    // Found!
                    await Stop();

                    CurrentMission = -1;
                }
            }
        }
    }
}
