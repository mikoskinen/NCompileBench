using System;
using System.Collections.Generic;
using CloudNative.CloudEvents;

namespace NCompileBench.Shared
{
    public class EncryptedResultCloudEventExtension : ICloudEventExtension
    {
        public const string EncryptedResultExtension = "ncompilebench.encryptedresult";

        IDictionary<string, object> _attributes = new Dictionary<string, object>();

        public string EncryptedResultValue
        {
            get => _attributes[EncryptedResultExtension] as string;
            set => _attributes[EncryptedResultExtension] = value;
        }

        public EncryptedResultCloudEventExtension(string encryptedResult)
        {
            EncryptedResultValue = encryptedResult;
        }

        public void Attach(CloudEvent cloudEvent)
        {
            var eventAttributes = cloudEvent.GetAttributes();
            if (_attributes == eventAttributes)
            {
                // already done
                return;
            }

            foreach (var attr in _attributes)
            {
                if (attr.Value != null)
                {
                    eventAttributes[attr.Key] = attr.Value;
                }
            }
            
            _attributes = eventAttributes;
        }

        public bool ValidateAndNormalize(string key, ref dynamic value)
        {
            if (string.Equals(key, EncryptedResultExtension))
            {
                if (value is string)
                {
                    return true;
                }

                throw new InvalidOperationException();
            }
            return false;
        }

        public Type GetAttributeType(string name)
        {
            if (string.Equals(name, EncryptedResultExtension))
            {
                return typeof(string);
            }

            return null;
        }
    }
    public class EncryptedKeyCloudEventExtension : ICloudEventExtension
    {
        public const string EncryptedKeyExtension = "ncompilebench.encryptedkey";

        IDictionary<string, object> _attributes = new Dictionary<string, object>();

        public string EncryptedKeyValue
        {
            get => _attributes[EncryptedKeyExtension] as string;
            set => _attributes[EncryptedKeyExtension] = value;
        }

        public EncryptedKeyCloudEventExtension(string encryptedKey)
        {
            EncryptedKeyValue = encryptedKey;
        }

        public void Attach(CloudEvent cloudEvent)
        {
            var eventAttributes = cloudEvent.GetAttributes();
            if (_attributes == eventAttributes)
            {
                // already done
                return;
            }

            foreach (var attr in _attributes)
            {
                if (attr.Value != null)
                {
                    eventAttributes[attr.Key] = attr.Value;
                }
            }
            
            _attributes = eventAttributes;
        }

        public bool ValidateAndNormalize(string key, ref dynamic value)
        {
            if (string.Equals(key, EncryptedKeyExtension))
            {
                if (value is string)
                {
                    return true;
                }

                throw new InvalidOperationException();
            }
            return false;
        }

        public Type GetAttributeType(string name)
        {
            if (string.Equals(name, EncryptedKeyExtension))
            {
                return typeof(string);
            }

            return null;
        }
    }
}
