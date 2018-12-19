﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LY.Domain
{
    /// <summary>
    ///  代表一个资源库。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    public interface IQueryRepository<TEntity> : IBaseQueryRepository<int, TEntity> where TEntity : Entity
    {
        
    }
}
