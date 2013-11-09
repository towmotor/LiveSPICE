﻿using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;

namespace Circuit
{
    /// <summary>
    /// Ideal voltage source.
    /// </summary>
    [Category("Standard")]
    [DisplayName("Voltage Source")]
    [DefaultProperty("Voltage")]
    [Description("Ideal voltage source.")]
    public class VoltageSource : TwoTerminal
    {
        /// <summary>
        /// Expression for voltage V.
        /// </summary>
        private Quantity voltage = new Quantity(Call.Sin(100 * 2 * 3.14 * t), Units.V);
        [Description("Voltage generated by this voltage source.")]
        [Serialize]
        public Quantity Voltage { get { return voltage; } set { if (voltage.Set(value)) NotifyChanged("Voltage"); } }

        /// <summary>
        /// True if the voltage expression for this voltage source is not defined at t = 0.
        /// </summary>
        [Browsable(false)]
        public bool IsInput { get { return !(Voltage.Value.Evaluate(Component.t, Constant.Zero) is Constant); } }

        public VoltageSource() { Name = "V1"; }
        
        public override void Analyze(ModifiedNodalAnalysis Mna)
        {
            // Unknown current.
            Mna.AddPassiveComponent(Anode, Cathode, Mna.AddNewUnknown("i" + Name));
            // Set the voltage.
            Mna.AddEquation(V, Voltage.Value);
            // Add initial conditions, if necessary.
            Expression V0 = Voltage.Value.Evaluate(t, Constant.Zero);
            if (!(V0 is Constant))
                Mna.AddInitialConditions(Arrow.New(V0, Constant.Zero)); 
        }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            base.LayoutSymbol(Sym);

            if (IsInput)
            {
                int w = 10;
                Sym.AddLine(EdgeType.Black, new Coord(-w, 20), new Coord(w, 20));
                Sym.DrawPositive(EdgeType.Black, new Coord(0, 15));
                Sym.AddLine(EdgeType.Black, new Coord(-w, -20), new Coord(w, -20));
                Sym.DrawNegative(EdgeType.Black, new Coord(0, -15));

                Sym.DrawText(() => Voltage.ToString(), new Point(0, 0), Alignment.Center, Alignment.Center);
            }
            else
            {
                int r = 10;

                Sym.AddWire(Anode, new Coord(0, r));
                Sym.AddWire(Cathode, new Coord(0, -r));

                Sym.AddCircle(EdgeType.Black, new Coord(0, 0), r);
                Sym.DrawPositive(EdgeType.Black, new Coord(0, 7));
                Sym.DrawNegative(EdgeType.Black, new Coord(0, -7));
                if (!(Voltage.Value is Constant))
                    Sym.DrawFunction(
                        EdgeType.Black,
                        (t) => t * r * 0.75,
                        (t) => Math.Sin(t * 3.1415) * r * 0.5, -1, 1);

                Sym.DrawText(() => Voltage.ToString(), new Point(r * 0.7, r * 0.7), Alignment.Near, Alignment.Near);
                Sym.DrawText(() => Name, new Point(r * 0.7, r * -0.7), Alignment.Near, Alignment.Far);
            }
        }

        public override string ToString() { return Name + " = " + Voltage.ToString(); }
    }

    /// <summary>
    /// An input is just a voltage source with Voltage = f(t).
    /// </summary>
    [Category("Standard")]
    [DisplayName("Input")]
    public class Input : VoltageSource
    {
        public Input() { Voltage = new Quantity("V[t]", Units.V); }
    };
}

