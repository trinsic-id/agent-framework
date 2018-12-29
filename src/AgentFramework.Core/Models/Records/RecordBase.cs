using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AgentFramework.Core.Extensions;
using Newtonsoft.Json;

namespace AgentFramework.Core.Models.Records
{
    /// <summary>
    /// Wallet record.
    /// </summary>
    public abstract class RecordBase
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        public virtual string Id { get; set; }

        [JsonIgnore]
        private DateTime _createdAt = DateTime.MinValue;

        /// <summary>
        /// Gets the created at datetime of the record.
        /// </summary>
        /// <returns>The created datetime of the record.</returns>
        [JsonIgnore]
        public DateTime? CreatedAt
        {
            get
            {
                if (_createdAt == DateTime.MinValue)
                    return null;
                return _createdAt;
            }
            internal set
            {
                if (value == null)
                    Set(DateTime.MinValue, ref _createdAt);
                else
                    Set(value.Value, ref _createdAt);
            }
        }

        [JsonIgnore]
        private DateTime _updatedAt = DateTime.MinValue;

        /// <summary>
        /// Gets the last updated datetime of the record.
        /// </summary>
        /// <returns>The last updated datetime of the record.</returns>
        [JsonIgnore]
        public DateTime? UpdatedAt
        {
            get
            {
                if (_updatedAt == DateTime.MinValue)
                    return null;
                return _updatedAt;
            }
            internal set
            {
                if (value == null)
                    Set(DateTime.MinValue, ref _updatedAt);
                else
                    Set(value.Value, ref _updatedAt);
            }
        }

        /// <summary>
        /// Gets the name of the type.
        /// </summary>
        /// <returns>The type name.</returns>
        [JsonIgnore]
        public abstract string TypeName { get; }

        [JsonIgnore]
        internal Dictionary<string, string> Tags { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets the attribute.
        /// </summary>
        /// <param name="name">Name.</param>
        public string GetTag(string name) => Get(name);

        /// <summary>
        /// Sets the attribute.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="value">Value.</param>
        public void SetTag(string name, string value) => Set(value, name);

        /// <summary>
        /// Removes a user attribute.
        /// </summary>
        /// <returns>The attribute.</returns>
        /// <param name="name">Name.</param>
        public void RemoveTag(string name) => Set(name, null);

        protected void Set(string value, [CallerMemberName]string name = "")
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name), "Attribute name must be specified.");
            }

            if (value != null)
            {
                Tags[name] = value;
            }
            else if (Tags.ContainsKey(name))
            {
                Tags.Remove(name);
            }
        }

        /// <summary>
        /// Set the specified value, field and name.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="field">Field.</param>
        /// <param name="name">Name.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        protected void Set<T>(T value, ref T field, [CallerMemberName]string name = "") where T : struct
        {
            if (typeof(T) == typeof(DateTime))
            {
                var dateVal = value as DateTime?;

                if (dateVal != null)
                {
                    var strVal = dateVal.ToUnixTimeMilliseconds().ToString();
                    Set(strVal, name);
                }
                else
                    Set(null,name);
            }
            else if (typeof(T).IsEnum)
            {
                Set((value as Enum).ToString("G"), name);
            }
            else
            {
                Set(value.ToString(), name);
            }
            field = value;
        }

        /// <summary>
        /// Get the value of the specified tag name.
        /// </summary>
        /// <returns>The get.</returns>
        /// <param name="name">Name.</param>
        protected string Get([CallerMemberName]string name = "")
        {
            if (Tags.ContainsKey(name))
            {
                return Tags[name];
            }
            return null;
        }
    }
}
