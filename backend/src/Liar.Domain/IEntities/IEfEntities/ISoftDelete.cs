﻿namespace Liar.Domain.IEntities
{
    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
    }
}
