using System;
using System.Linq;
using System.Reflection;

public static class InterfaceImplementationChecker
{
    public static void CheckImplementation<TInterface, TClass>()
    {
        Type interfaceType = typeof(TInterface);
        Type classType = typeof(TClass);

        var missingMembers = interfaceType.GetMembers()
            .Where(interfaceMember => !classType.GetMembers().Any(classMember =>
                classMember.Name == interfaceMember.Name &&
                classMember.MemberType == interfaceMember.MemberType));

        if (missingMembers.Any())
        {
            Console.WriteLine($"The class {classType.Name} is missing the following implementations from {interfaceType.Name}:");
            foreach (var member in missingMembers)
            {
                Console.WriteLine($"- {member.MemberType} {member.Name}");
            }
        }
        else
        {
            Console.WriteLine($"The class {classType.Name} fully implements {interfaceType.Name}.");
        }
    }
}