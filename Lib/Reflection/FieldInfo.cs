﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace Souvenir.Reflection
{
    class FieldInfo<T> : InfoBase<T>
    {
        public readonly FieldInfo Field;
        protected override string LoggingString => $"Field {Field.DeclaringType.FullName}.{Field.Name}";
        public FieldInfo(object target, FieldInfo field) : base(target) { Field = field; }

        protected override T GetValue() => (T) Field.GetValue(_target);
        protected override T GetValue(object from) => (T) Field.GetValue(from);
        public void Set(T value) { Field.SetValue(_target, value); }
        public void SetTo(object target, T value) { Field.SetValue(target, value); }
    }

    sealed class IntFieldInfo : FieldInfo<int>
    {
        public IntFieldInfo(object target, FieldInfo field) : base(target, field) { }

        public int Get(int? min = null, int? max = null)
        {
            return Get(v => (min != null && v < min.Value) || (max != null && v > max.Value) ? string.Format("expected {0}–{1}", min, max) : null);
        }

        public int GetFrom(object obj, int? min = null, int? max = null)
        {
            return GetFrom(obj, v => (min != null && v < min.Value) || (max != null && v > max.Value) ? string.Format("expected {0}–{1}", min, max) : null);
        }
    }

    abstract class CollectionFieldInfo<TCollection, TElement> : FieldInfo<TCollection> where TCollection : IList<TElement>
    {
        protected CollectionFieldInfo(object target, FieldInfo field) : base(target, field) { }

        public TCollection Get(int expectedLength, bool nullArrayAllowed = false, bool nullContentAllowed = false, Func<TElement, string> validator = null)
        {
            return GetFrom(_target, expectedLength, expectedLength, nullArrayAllowed, nullContentAllowed, validator);
        }

        public TCollection Get(int minLength, int? maxLength = null, bool nullArrayAllowed = false, bool nullContentAllowed = false, Func<TElement, string> validator = null)
        {
            return GetFrom(_target, minLength, maxLength, nullArrayAllowed, nullContentAllowed, validator);
        }

        public TCollection GetFrom(object target, int expectedLength, bool nullArrayAllowed = false, bool nullContentAllowed = false, Func<TElement, string> validator = null)
        {
            return GetFrom(target, expectedLength, expectedLength, nullArrayAllowed, nullContentAllowed, validator);
        }

        public TCollection GetFrom(object target, int minLength, int? maxLength = null, bool nullArrayAllowed = false, bool nullContentAllowed = false, Func<TElement, string> validator = null)
        {
            var collection = base.GetFrom(target, nullAllowed: nullArrayAllowed);
            if (collection == null)
                return collection;
            if (collection.Count < minLength || (maxLength != null && collection.Count > maxLength.Value))
                throw new AbandonModuleException("Collection field {0}.{1} has length {2} (expected {3}{4}).", Field.DeclaringType.FullName, Field.Name, collection.Count,
                    maxLength == null ? "at least " : minLength.ToString(),
                    maxLength == null ? minLength.ToString() : maxLength.Value != minLength ? "–" + maxLength.Value : "");
            int pos;
            if (!nullContentAllowed && (pos = collection.IndexOf(v => v == null)) != -1)
                throw new AbandonModuleException("Collection field {0}.{1} (length {2}) contained a null value at index {3}.", Field.DeclaringType.FullName, Field.Name, collection.Count, pos);
            string validatorFailMessage;
            if (validator != null)
                for (var ix = 0; ix < collection.Count; ix++)
                    if ((validatorFailMessage = validator(collection[ix])) != null)
                        throw new AbandonModuleException("Collection field {0}.{1} (length {2}) contained value {3} at index {4} that failed the validator: {5}.",
                            Field.DeclaringType.FullName, Field.Name, collection.Count, stringify(collection[ix]), ix, validatorFailMessage);
            return collection;
        }

        public new TCollection Get(Func<TCollection, string> validator = null, bool nullAllowed = false)
        {
            var collection = base.Get(validator, nullAllowed);
            if (collection == null)
                return collection;
            var pos = collection.IndexOf(v => v == null);
            if (pos != -1)
                throw new AbandonModuleException("Collection field {0}.{1} (length {2}) contained a null value at index {3}.", Field.DeclaringType.FullName, Field.Name, collection.Count, pos);
            return collection;
        }

        public new TCollection GetFrom(object obj, Func<TCollection, string> validator = null, bool nullAllowed = false)
        {
            var collection = base.GetFrom(obj, validator, nullAllowed);
            if (collection == null)
                return collection;
            var pos = collection.IndexOf(v => v == null);
            if (pos != -1)
                throw new AbandonModuleException("Collection field {0}.{1} (length {2}) contained a null value at index {3}.", Field.DeclaringType.FullName, Field.Name, collection.Count, pos);
            return collection;
        }
    }

    sealed class ArrayFieldInfo<T> : CollectionFieldInfo<T[], T>
    {
        public ArrayFieldInfo(object target, FieldInfo field) : base(target, field) { }
    }

    sealed class ListFieldInfo<T> : CollectionFieldInfo<List<T>, T>
    {
        public ListFieldInfo(object target, FieldInfo field) : base(target, field) { }
    }
}
