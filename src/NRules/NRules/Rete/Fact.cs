﻿using System;
using System.Diagnostics;
using System.Reflection;
using NRules.RuleModel;

namespace NRules.Rete
{
    [DebuggerDisplay("Fact {Object}")]
    internal class Fact : IFact
    {
        private object _object;

        public Fact()
        {
        }

        public Fact(object @object)
        {
            _object = @object;
            var factType = @object.GetType();
            FactType = factType.GetTypeInfo();
        }

        public virtual TypeInfo FactType { get; }

        public object RawObject
        {
            get => _object;
            set => _object = value;
        }

        public virtual object Object => _object;
        public virtual bool IsWrapperFact => false;
        Type IFact.Type => FactType.AsType();
        object IFact.Value => Object;
    }

    [DebuggerDisplay("Wrapper Tuple({WrappedTuple.Count})")]
    internal class WrapperFact : Fact
    {
        public WrapperFact(Tuple tuple)
            : base(tuple)
        {
        }

        public override TypeInfo FactType => WrappedTuple.RightFact.FactType;
        public override object Object => WrappedTuple.RightFact.Object;
        public Tuple WrappedTuple => (Tuple) RawObject;
        public override bool IsWrapperFact => true;
    }
}