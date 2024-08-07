﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using NetSparkle.Interfaces;

namespace NetSparkle;

/// <summary>
///     Assembly reflection accessor
/// </summary>
public class NetSparkleAssemblyReflectionAccessor : INetSparkleAssemblyAccessor
{
    private readonly Assembly? _assembly;
    private readonly List<Attribute> _assemblyAttributes = [];

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="assemblyName">the assembly name</param>
    public NetSparkleAssemblyReflectionAccessor(string? assemblyName)
    {
        if (string.IsNullOrEmpty(assemblyName))
        {
            _assembly = Assembly.GetEntryAssembly();
            GetAssemblyAttributes(_assembly);
        }
        else
        {
            var absolutePath = Path.GetFullPath(assemblyName);
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException();
            }

            var resolver = new PathAssemblyResolver(new List<string> { absolutePath });
            var mlc = new MetadataLoadContext(resolver);

            using (mlc)
            {
                _assembly = mlc.LoadFromAssemblyPath(absolutePath);

                GetAssemblyAttributes(_assembly);
            }
        }

        if (_assemblyAttributes == null || _assemblyAttributes.Count == 0)
        {
            throw new ArgumentOutOfRangeException($"Unable to load assembly attributes from {_assembly?.FullName}");
        }

        return;

        void GetAssemblyAttributes(Assembly? assembly)
        {
            if (assembly == null)
            {
                return;
            }

            foreach (var data in assembly.GetCustomAttributesData())
            {
                _assemblyAttributes.Add(CreateAttribute(data));
            }
        }
    }

    /// <summary>
    ///     This methods creates an attribute instance from the attribute data
    ///     information
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private static Attribute? CreateAttribute(CustomAttributeData data)
    {
        var arguments = from arg in data.ConstructorArguments select arg.Value;

        var attribute = data.Constructor.Invoke(arguments.ToArray())
            as Attribute;

        if (data.NamedArguments == null)
        {
            return attribute;
        }

        foreach (var namedArgument in data.NamedArguments)
        {
            var propertyInfo = namedArgument.MemberInfo as PropertyInfo;
            if (propertyInfo != null)
            {
                propertyInfo.SetValue(attribute, namedArgument.TypedValue.Value, null);
            }
            else
            {
                var fieldInfo = namedArgument.MemberInfo as FieldInfo;
                fieldInfo?.SetValue(attribute, namedArgument.TypedValue.Value);
            }
        }

        return attribute;
    }

    private Attribute FindAttribute(Type attributeType)
    {
        foreach (var attr in _assemblyAttributes.Where(attr => attr.GetType() == attributeType))
        {
            return attr;
        }

        throw new Exception($"Attribute of type {attributeType} does not exists in the assembly {_assembly?.FullName}");
    }

    #region Assembly Attribute Accessors

    /// <summary>
    ///     Gets the assembly title
    /// </summary>
    public string? AssemblyTitle
    {
        get
        {
            var a = FindAttribute(typeof(AssemblyTitleAttribute)) as AssemblyTitleAttribute;
            return a?.Title;
        }
    }

    /// <summary>
    ///     Gets the version
    /// </summary>
    public string AssemblyVersion
    {
        get
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fileVersionInfo.ProductVersion!;
        }
    }

    /// <summary>
    ///     Gets the description
    /// </summary>
    public string? AssemblyDescription
    {
        get
        {
            var a = FindAttribute(typeof(AssemblyDescriptionAttribute)) as AssemblyDescriptionAttribute;
            return a?.Description;
        }
    }

    /// <summary>
    ///     Gets the product
    /// </summary>
    public string? AssemblyProduct
    {
        get
        {
            var a = FindAttribute(typeof(AssemblyProductAttribute)) as AssemblyProductAttribute;
            return a?.Product;
        }
    }

    /// <summary>
    ///     Gets the copyright
    /// </summary>
    public string? AssemblyCopyright
    {
        get
        {
            var a = FindAttribute(typeof(AssemblyCopyrightAttribute)) as AssemblyCopyrightAttribute;
            return a?.Copyright;
        }
    }

    /// <summary>
    ///     Gets the company
    /// </summary>
    public string? AssemblyCompany
    {
        get
        {
            var a = FindAttribute(typeof(AssemblyCompanyAttribute)) as AssemblyCompanyAttribute;
            return a?.Company;
        }
    }

    #endregion
}
