using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;

    [Header("Engine & Performance")]
    public float motorPower = 1200f;
    public float brakePower = 3500f;
    public float maxSpeed = 120f; // km/h
    public AnimationCurve powerCurve = AnimationCurve.EaseInOut(0, 1, 1, 0.6f);

    [Header("Steering")]
    public float maxSteerAngle = 30f;
    public float steeringSpeed = 5f;
    public AnimationCurve steeringSensitivityCurve = AnimationCurve.Linear(0, 1, 100, 0.3f);

    [Header("Drivetrain")]
    public DriveType driveType = DriveType.RearWheelDrive;
    public float frontRearPowerSplit = 0.5f; // For AWD: 0.5 = 50/50

    [Header("Stability & Control")]
    public float downforceMultiplier = 2f;
    public float tractionControl = 0.8f; // 0 = off, 1 = full
    public float antiRollStrength = 5000f;
    public Vector3 centerOfMass = new Vector3(0, -0.6f, 0.2f);

    [Header("Advanced Tuning")]
    public float corneringStiffness = 1.2f;
    public float rollingResistance = 30f;

    private Rigidbody rb;
    private float currentSpeed;
    private float currentSteerAngle;
    private float motorInput;
    private float steerInput;
    private bool isBraking;

    public enum DriveType { FrontWheelDrive, RearWheelDrive, AllWheelDrive }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        SetupRigidbody();

        if (!ValidateSetup())
        {
            enabled = false;
            return;
        }

        ConfigureWheels();
    }

    void SetupRigidbody()
    {
        rb.mass = 1200f;
        rb.linearDamping = 0.02f;
        rb.angularDamping = 0.5f;
        rb.centerOfMass = centerOfMass;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    bool ValidateSetup()
    {
        if (frontLeftWheel == null || frontRightWheel == null ||
            rearLeftWheel == null || rearRightWheel == null)
        {
            Debug.LogError("CarController: All 4 wheel colliders must be assigned!");
            return false;
        }
        return true;
    }

    void ConfigureWheels()
    {
        ConfigureWheel(frontLeftWheel, true);
        ConfigureWheel(frontRightWheel, true);
        ConfigureWheel(rearLeftWheel, false);
        ConfigureWheel(rearRightWheel, false);
    }

    void ConfigureWheel(WheelCollider wheel, bool isFrontWheel)
    {
        wheel.mass = 25f;
        wheel.radius = 0.35f;
        wheel.wheelDampingRate = 1.5f;
        wheel.suspensionDistance = 0.15f;
        wheel.forceAppPointDistance = 0f;

        // Suspension spring
        JointSpring spring = wheel.suspensionSpring;
        spring.spring = 40000f;
        spring.damper = 5000f;
        spring.targetPosition = 0.5f;
        wheel.suspensionSpring = spring;

        // Forward friction (traction)
        WheelFrictionCurve forwardFriction = new WheelFrictionCurve();
        forwardFriction.extremumSlip = 0.3f;
        forwardFriction.extremumValue = 1.2f;
        forwardFriction.asymptoteSlip = 0.7f;
        forwardFriction.asymptoteValue = 0.8f;
        forwardFriction.stiffness = 2.5f;
        wheel.forwardFriction = forwardFriction;

        // Sideways friction (lateral grip)
        WheelFrictionCurve sidewaysFriction = new WheelFrictionCurve();
        sidewaysFriction.extremumSlip = 0.2f;
        sidewaysFriction.extremumValue = isFrontWheel ? 1.5f : 1.3f;
        sidewaysFriction.asymptoteSlip = 0.5f;
        sidewaysFriction.asymptoteValue = isFrontWheel ? 1.2f : 1f;
        sidewaysFriction.stiffness = 3.5f * corneringStiffness;
        wheel.sidewaysFriction = sidewaysFriction;
    }

    void Update()
    {
        GetInput();
    }

    void FixedUpdate()
    {
        currentSpeed = rb.linearVelocity.magnitude * 3.6f;

        ApplySteering();
        ApplyMotor();
        ApplyBrakes();
        ApplyDownforce();
        ApplyAntiRoll();
        ApplyRollingResistance();
    }

    void GetInput()
    {
        motorInput = Input.GetAxis("Vertical");
        steerInput = -Input.GetAxis("Horizontal");
        isBraking = Input.GetKey(KeyCode.Space);
    }

    void ApplySteering()
    {
        // Speed-sensitive steering
        float speedNormalized = Mathf.Clamp01(currentSpeed / 100f);
        float steeringSensitivity = steeringSensitivityCurve.Evaluate(currentSpeed);

        float targetSteer = steerInput * maxSteerAngle * steeringSensitivity;
        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteer, Time.fixedDeltaTime * steeringSpeed);

        frontLeftWheel.steerAngle = currentSteerAngle;
        frontRightWheel.steerAngle = currentSteerAngle;
    }

    void ApplyMotor()
    {
        if (currentSpeed >= maxSpeed && motorInput > 0)
        {
            ZeroMotorTorque();
            return;
        }

        // Power curve based on speed
        float speedFactor = Mathf.Clamp01(currentSpeed / maxSpeed);
        float powerMultiplier = powerCurve.Evaluate(speedFactor);

        float torque = motorInput * motorPower * powerMultiplier;

        // Apply traction control
        torque = ApplyTractionControl(torque);

        // Distribute power based on drivetrain
        switch (driveType)
        {
            case DriveType.FrontWheelDrive:
                ApplyTorqueToWheels(frontLeftWheel, frontRightWheel, torque);
                ZeroTorqueToWheels(rearLeftWheel, rearRightWheel);
                break;

            case DriveType.RearWheelDrive:
                ZeroTorqueToWheels(frontLeftWheel, frontRightWheel);
                ApplyTorqueToWheels(rearLeftWheel, rearRightWheel, torque);
                break;

            case DriveType.AllWheelDrive:
                float frontTorque = torque * frontRearPowerSplit;
                float rearTorque = torque * (1f - frontRearPowerSplit);
                ApplyTorqueToWheels(frontLeftWheel, frontRightWheel, frontTorque);
                ApplyTorqueToWheels(rearLeftWheel, rearRightWheel, rearTorque);
                break;
        }
    }

    float ApplyTractionControl(float torque)
    {
        if (tractionControl <= 0) return torque;

        // Check if wheels are slipping
        float avgSlip = 0f;
        int wheelCount = 0;

        WheelHit hit;
        foreach (WheelCollider wheel in new[] { frontLeftWheel, frontRightWheel, rearLeftWheel, rearRightWheel })
        {
            if (wheel.GetGroundHit(out hit))
            {
                avgSlip += Mathf.Abs(hit.forwardSlip);
                wheelCount++;
            }
        }

        if (wheelCount > 0)
        {
            avgSlip /= wheelCount;

            // Reduce power if slipping
            if (avgSlip > 0.3f)
            {
                float reduction = Mathf.Lerp(1f, 0.5f, (avgSlip - 0.3f) * tractionControl);
                torque *= reduction;
            }
        }

        return torque;
    }

    void ApplyTorqueToWheels(WheelCollider left, WheelCollider right, float torque)
    {
        left.motorTorque = torque * 0.5f;
        right.motorTorque = torque * 0.5f;
    }

    void ZeroTorqueToWheels(WheelCollider left, WheelCollider right)
    {
        left.motorTorque = 0;
        right.motorTorque = 0;
    }

    void ZeroMotorTorque()
    {
        frontLeftWheel.motorTorque = 0;
        frontRightWheel.motorTorque = 0;
        rearLeftWheel.motorTorque = 0;
        rearRightWheel.motorTorque = 0;
    }

    void ApplyBrakes()
    {
        float brakeForce = 0f;

        if (isBraking)
        {
            // Handbrake - rear wheels only
            brakeForce = brakePower * 1.5f;
            rearLeftWheel.brakeTorque = brakeForce;
            rearRightWheel.brakeTorque = brakeForce;
            frontLeftWheel.brakeTorque = 0;
            frontRightWheel.brakeTorque = 0;
            return;
        }

        // Brake when trying to go opposite direction
        bool shouldBrake = (motorInput < -0.1f && currentSpeed > 2f) ||
                          (motorInput > 0.1f && rb.linearVelocity.magnitude > 0.5f && Vector3.Dot(rb.linearVelocity, transform.forward) < -0.5f);

        if (shouldBrake)
        {
            brakeForce = brakePower;
        }
        else if (Mathf.Abs(motorInput) < 0.1f && currentSpeed > 1f)
        {
            // Idle braking
            brakeForce = rollingResistance;
        }

        frontLeftWheel.brakeTorque = brakeForce;
        frontRightWheel.brakeTorque = brakeForce;
        rearLeftWheel.brakeTorque = brakeForce;
        rearRightWheel.brakeTorque = brakeForce;
    }

    void ApplyDownforce()
    {
        float speedFactor = rb.linearVelocity.magnitude;
        float downforce = speedFactor * downforceMultiplier;
        rb.AddForce(-transform.up * downforce, ForceMode.Force);
    }

    void ApplyAntiRoll()
    {
        ApplyAntiRollBar(frontLeftWheel, frontRightWheel);
        ApplyAntiRollBar(rearLeftWheel, rearRightWheel);
    }

    void ApplyAntiRollBar(WheelCollider leftWheel, WheelCollider rightWheel)
    {
        WheelHit hit;
        float travelL = 1f;
        float travelR = 1f;

        bool groundedL = leftWheel.GetGroundHit(out hit);
        if (groundedL)
        {
            travelL = (-leftWheel.transform.InverseTransformPoint(hit.point).y - leftWheel.radius) / leftWheel.suspensionDistance;
        }

        bool groundedR = rightWheel.GetGroundHit(out hit);
        if (groundedR)
        {
            travelR = (-rightWheel.transform.InverseTransformPoint(hit.point).y - rightWheel.radius) / rightWheel.suspensionDistance;
        }

        float antiRollForce = (travelL - travelR) * antiRollStrength;

        if (groundedL)
        {
            rb.AddForceAtPosition(leftWheel.transform.up * -antiRollForce, leftWheel.transform.position);
        }
        if (groundedR)
        {
            rb.AddForceAtPosition(rightWheel.transform.up * antiRollForce, rightWheel.transform.position);
        }
    }

    void ApplyRollingResistance()
    {
        // Natural deceleration
        if (Mathf.Abs(motorInput) < 0.1f)
        {
            Vector3 resistance = -rb.linearVelocity.normalized * rollingResistance;
            rb.AddForce(resistance, ForceMode.Force);
        }
    }

    // Public API
    public float GetSpeed() => currentSpeed;
    public float GetSpeedMPH() => currentSpeed * 0.621371f;
    public float GetSpeedNormalized() => Mathf.Clamp01(currentSpeed / maxSpeed);
    public float GetRPM() => Mathf.Clamp(1000f + (currentSpeed / maxSpeed) * 6000f, 1000f, 7000f);
    public int GetGear()
    {
        float speedRatio = currentSpeed / maxSpeed;
        if (speedRatio < 0.15f) return 1;
        if (speedRatio < 0.35f) return 2;
        if (speedRatio < 0.55f) return 3;
        if (speedRatio < 0.75f) return 4;
        return 5;
    }
}