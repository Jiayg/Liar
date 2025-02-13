﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Liar.Application.Contracts.Dtos.Sys.Dept;
using Liar.Application.Contracts.IServices.Sys;
using Liar.HttpApi.Authorize;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Liar.HttpApi.Host.Controllers
{
    /// <summary>
    /// 部门管理
    /// </summary>
    [Route("depts")]
    [ApiController]
    public class DeptController : BaseController
    {
        private readonly IDeptService _deptService;

        public DeptController(IDeptService deptService)
        {
            _deptService = deptService;
        }

        /// <summary>
        /// 删除部门
        /// </summary>
        /// <param name="id">部门ID</param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [Permission("deptDelete")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> Delete([FromRoute] long id)
        {
            return Result(await _deptService.DeleteAsync(id));
        }

        /// <summary>
        /// 新增部门
        /// </summary>
        /// <param name="input">部门</param>
        /// <returns></returns>
        [HttpPost]
        [Permission("deptAdd")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<ActionResult<long>> CreateAsync([FromBody] DeptCreationDto input)
        {
            return CreatedResult(await _deptService.CreateAsync(input));
        }

        /// <summary>
        /// 修改部门
        /// </summary>
        /// <param name="id">id</param>
        /// <param name="input">部门</param>
        /// <returns></returns>
        [HttpPut("{id}")]
        [Permission("deptEdit")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult<long>> UpdateAsync([FromRoute] long id, [FromBody] DeptUpdationDto input)
        {
            return Result(await _deptService.UpdateAsync(id, input));
        }

        /// <summary>
        /// 获取部门列表
        /// </summary>
        /// <returns></returns>
        [HttpGet()]
        [Permission("deptList")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DeptTreeDto>>> GetListAsync()
        {
            return await _deptService.GetTreeListAsync();
        }
    }
}
