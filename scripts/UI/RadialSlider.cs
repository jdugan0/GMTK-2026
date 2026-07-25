using Godot;

[Tool]
[GlobalClass]
public partial class RadialSlider : Control
{
    [Signal]
    public delegate void ValueChangedEventHandler(double value);

    [Signal]
    public delegate void DragStartedEventHandler();

    [Signal]
    public delegate void DragEndedEventHandler(bool valueChanged);

    private double minValue;
    private double maxValue = 1;
    private double step = 0.01;
    private double currentValue;

    private Color fillColor = new(1f, 0f, 0f, 1f);
    private Color trackColor = new(0f, 0f, 0f, 0f);
    private float radius;
    private float thickness = 24f;
    private float startDegrees = -90f;
    private float sweepDegrees = 360f;
    private bool clockwise = true;
    private bool showHandle = true;
    private float handleRadius = 16f;

    private bool dragging;
    private double dragStartValue;

    [Export]
    public double MinValue
    {
        get => minValue;
        set
        {
            minValue = value;
            Value = currentValue;
            QueueRedraw();
        }
    }

    [Export]
    public double MaxValue
    {
        get => maxValue;
        set
        {
            maxValue = value;
            Value = currentValue;
            QueueRedraw();
        }
    }

    [Export]
    public double Step
    {
        get => step;
        set
        {
            step = Mathf.Max(0.0, value);
            Value = currentValue;
        }
    }

    [Export]
    public double Value
    {
        get => currentValue;
        set
        {
            double snapped = step > 0.0 ? Mathf.Snapped(value, step) : value;
            snapped = Mathf.Clamp(snapped, minValue, maxValue);
            if (Mathf.IsEqualApprox(snapped, currentValue))
                return;

            currentValue = snapped;
            QueueRedraw();
            EmitSignal(SignalName.ValueChanged, snapped);
        }
    }

    public double Ratio
    {
        get => maxValue > minValue ? (currentValue - minValue) / (maxValue - minValue) : 0.0;
        set => Value = minValue + Mathf.Clamp(value, 0.0, 1.0) * (maxValue - minValue);
    }

    [Export]
    public Color FillColor
    {
        get => fillColor;
        set
        {
            fillColor = value;
            QueueRedraw();
        }
    }

    [Export]
    public Color TrackColor
    {
        get => trackColor;
        set
        {
            trackColor = value;
            QueueRedraw();
        }
    }

    [Export(PropertyHint.Range, "0,512,1,or_greater")]
    public float Radius
    {
        get => radius;
        set
        {
            radius = Mathf.Max(0f, value);
            UpdateMinimumSize();
            QueueRedraw();
        }
    }

    [Export(PropertyHint.Range, "1,256,1,or_greater")]
    public float Thickness
    {
        get => thickness;
        set
        {
            thickness = Mathf.Max(1f, value);
            UpdateMinimumSize();
            QueueRedraw();
        }
    }

    [Export(PropertyHint.Range, "-360,360,1")]
    public float StartDegrees
    {
        get => startDegrees;
        set
        {
            startDegrees = value;
            QueueRedraw();
        }
    }

    [Export(PropertyHint.Range, "1,360,1")]
    public float SweepDegrees
    {
        get => sweepDegrees;
        set
        {
            sweepDegrees = Mathf.Clamp(value, 1f, 360f);
            QueueRedraw();
        }
    }

    [Export]
    public bool Clockwise
    {
        get => clockwise;
        set
        {
            clockwise = value;
            QueueRedraw();
        }
    }

    [Export]
    public bool ShowHandle
    {
        get => showHandle;
        set
        {
            showHandle = value;
            QueueRedraw();
        }
    }

    [Export(PropertyHint.Range, "0,64,1,or_greater")]
    public float HandleRadius
    {
        get => handleRadius;
        set
        {
            handleRadius = Mathf.Max(0f, value);
            QueueRedraw();
        }
    }

    [Export]
    public bool Draggable { get; set; } = true;

    [Export(PropertyHint.Range, "0,64,1,or_greater")]
    public float GrabTolerance { get; set; } = 12f;

    private float EffectiveRadius =>
        radius > 0f ? radius : Mathf.Max(0f, (Mathf.Min(Size.X, Size.Y) - thickness) * 0.5f);

    public override Vector2 _GetMinimumSize()
    {
        float diameter = radius * 2f + thickness;
        return new Vector2(diameter, diameter);
    }

    public override void _Draw()
    {
        float r = EffectiveRadius;
        if (r <= 0f)
            return;

        Vector2 center = Size * 0.5f;
        float start = Mathf.DegToRad(startDegrees);
        float sweep = Mathf.DegToRad(sweepDegrees) * (clockwise ? 1f : -1f);

        if (trackColor.A > 0f)
            DrawArc(center, r, start, start + sweep, ArcPoints(sweep), trackColor, thickness, true);

        float filled = sweep * (float)Ratio;
        if (!Mathf.IsZeroApprox(filled))
            DrawArc(center, r, start, start + filled, ArcPoints(filled), fillColor, thickness, true);

        if (showHandle && handleRadius > 0f)
            DrawCircle(center + Vector2.FromAngle(start + filled) * r, handleRadius, fillColor);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (!Draggable)
            return;

        if (@event is InputEventMouseButton button && button.ButtonIndex == MouseButton.Left)
        {
            if (button.Pressed)
            {
                if (!OnRing(button.Position))
                    return;

                dragging = true;
                dragStartValue = currentValue;
                EmitSignal(SignalName.DragStarted);
                SetValueFromPoint(button.Position);
                AcceptEvent();
            }
            else if (dragging)
            {
                dragging = false;
                EmitSignal(SignalName.DragEnded, !Mathf.IsEqualApprox(currentValue, dragStartValue));
                AcceptEvent();
            }
        }
        else if (dragging && @event is InputEventMouseMotion motion)
        {
            SetValueFromPoint(motion.Position);
            AcceptEvent();
        }
    }

    private bool OnRing(Vector2 point)
    {
        float r = EffectiveRadius;
        float distance = point.DistanceTo(Size * 0.5f);
        float slack = thickness * 0.5f + Mathf.Max(GrabTolerance, handleRadius);
        return distance >= r - slack && distance <= r + slack;
    }

    private void SetValueFromPoint(Vector2 point)
    {
        Vector2 offset = point - Size * 0.5f;
        if (offset.LengthSquared() < 0.01f)
            return;

        float start = Mathf.DegToRad(startDegrees);
        float sweep = Mathf.DegToRad(sweepDegrees);
        float angle = offset.Angle() - start;
        if (!clockwise)
            angle = -angle;

        angle = Mathf.PosMod(angle, Mathf.Tau);
        if (angle > sweep)
            angle = angle - sweep < Mathf.Tau - angle ? sweep : 0f;

        Ratio = angle / sweep;
    }

    private static int ArcPoints(float angle) =>
        Mathf.Max(2, Mathf.RoundToInt(Mathf.Abs(angle) / Mathf.Tau * 128f));
}
