using HexGame.Grid;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexGame.Resources
{
    [System.Serializable]
    public struct ResourceAmount :IEqualityComparer<ResourceAmount>, IEquatable<ResourceAmount>
    {
        public ResourceAmount(ResourceType type, int amount)
        {
            this.type = type;
            this.amount = amount;
        }

        public ResourceType type;
        public int amount;

        public static ResourceAmount operator +(ResourceAmount a, ResourceAmount b)
        {
            return new ResourceAmount(a.type, a.amount + b.amount);
        }

        public static ResourceAmount operator -(ResourceAmount a, ResourceAmount b)
        {
            return new ResourceAmount(a.type, a.amount - b.amount);
        }

        public override string ToString()
        {
            return $"{type} {amount}";
        }

        public string ToPrettyString()
        {
            return $"Type: {type} Amount: {amount}";
        }

        public void ClearResource()
        {
            amount = 0;
        }

        public static bool operator ==(ResourceAmount h1, ResourceAmount h2)
        {
            return h1.type == h2.type && h1.amount == h2.amount;
        }
        public static bool operator !=(ResourceAmount h1, ResourceAmount h2)
        {
            return h1.type != h2.type || h1.amount != h2.amount;
        }

        public bool Equals(ResourceAmount x, ResourceAmount y)
        {
            return x.type == y.type && x.amount == y.amount;
        }

        public int GetHashCode(ResourceAmount obj)
        {
            return HashCode.Combine(obj.type, obj.amount);
        }

        public bool Equals(ResourceAmount other)
        {
            return this.type == other.type && this.amount == other.amount;
        }
    }
}
