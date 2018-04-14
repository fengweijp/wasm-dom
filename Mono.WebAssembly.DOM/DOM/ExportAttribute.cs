

// based on code of System.Composition.ExportAttribute
using System;

namespace Mono.WebAssembly
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method, 
                    AllowMultiple = true, Inherited = false)]
    public class ExportAttribute : Attribute
    {
        public ExportAttribute() : this(null, null)
        {
        }

        public ExportAttribute(Type contractType) : this(null, contractType)
        {
        }

        public ExportAttribute(string contractName) : this(contractName, null)
        {
        }

        public ExportAttribute(string contractName, Type contractType)
        {
            ContractName = contractName;
            ContractType = contractType;
        }

        public string ContractName { get; }

        public Type ContractType { get; }
    }
}
