
using System;
using System.Numerics;
using Vector2 = Godot.Vector2;

namespace PostEuclid.poincare;


public class MobiusTransform {
        
    public Complex a;
    public Complex b;
    public Complex c;
    public Complex d;
    
    public static MobiusTransform identity() {
        return new MobiusTransform(1, 0,
                            0, 1);
    }
    
    public static MobiusTransform translation(double dx, double dy) {
        Complex v = new Complex(dx, dy);
        
        return new MobiusTransform(1,           v,
                                   Complex.Conjugate(v), 1);
    }
    
    public static MobiusTransform rotation(double angle) {
        return new MobiusTransform(
            new Complex(Math.Cos(angle), Math.Sin(angle)),   0,
            0,                                           1);

    }

    public MobiusTransform(MobiusTransform other)
    {
        this.a = other.a;
        this.b = other.b;
        this.c = other.c;
        this.d = other.d;
    }

    private MobiusTransform(Complex a, Complex b, Complex c, Complex d) {
        this.a = a;
        this.b = b;
        this.c = c;
        this.d = d;
    }
    
    public static MobiusTransform Multiply(MobiusTransform left, MobiusTransform right) {
        Complex a = left.a;
        Complex b = left.b;
        Complex c = left.c;
        Complex d = left.d;
        Complex e = right.a;
        Complex f = right.b;
        Complex g = right.c;
        Complex h = right.d;
        
        return new MobiusTransform(
            a * e + b * g,          a * f + b * h,
            c * e + d * g,          c * f + d * h
        );
    }

    public Vector2 transform_point(Vector2 point)
    {
        var z = new Complex(point.X, point.Y);
        var p_new = (a * z + b) / (c * z + d);

        return new Vector2((float)p_new.Real, (float)p_new.Imaginary);
    }

    public MobiusTransform Inverse()
    {
        var determinant = a * d - b * c;

        if (determinant == 0)
        {
            throw new InvalidOperationException("Not Invertible");
        }

        var inverse = 1.0 / determinant;

        return new MobiusTransform(
            d * inverse, -b * inverse,
            -c * inverse, a * inverse);
    }
}
