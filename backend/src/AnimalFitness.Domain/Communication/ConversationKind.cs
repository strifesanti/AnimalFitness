namespace AnimalFitness.Domain.Communication;

/// <summary>Tipo de conversación.</summary>
public enum ConversationKind
{
    /// <summary>Chat privado 1:1 o grupo pequeño de la academia.</summary>
    Direct = 1,

    /// <summary>Canal de la organización.</summary>
    Group = 2,
}
