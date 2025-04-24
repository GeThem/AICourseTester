using Microsoft.AspNetCore.Mvc;
using NuGet.Protocol;
using System.Text.Json;
using AICourseTester.Models;
using AICourseTester.Services;
using AICourseTester.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Xml.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.IdentityModel.Tokens;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AICourseTester.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ABController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly MainDbContext _context;

        public ABController(MainDbContext context, UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
            _context = context;
        }

        [HttpGet("Train")]
        public ProblemTree<ABNode> GetABTrain([System.Web.Http.FromUri] int depth = 3)
        {
            ABNode aNode = new ABNode();
            var tree = AlphaBetaService.GenerateTree(aNode, depth);
            return tree;
        }

        //[HttpGet("Train/Test")]
        //public ProblemTree<ABNode> GetABTrainTest()
        //{
        //    ABNode aNode = new ABNode();
        //    aNode.SubNodes = [new ABNode(), new ABNode()];
        //    int i = -1;
        //    int[] vals = [ 10, 5, 7, 11, 12, 8, 9, 8, 5, 12, 11, 12, 9, 8, 7, 10 ];
        //    foreach (var node1 in aNode.SubNodes)
        //    {
        //        node1.SubNodes = [new ABNode(), new ABNode()];
        //        foreach (var node2 in node1.SubNodes)
        //        {
        //            node2.SubNodes = [new ABNode(), new ABNode()];
        //            foreach (var node3 in node2.SubNodes)
        //            {
        //                node3.SubNodes = [new ABNode() { A=vals[++i], B=vals[i] }, new ABNode() { A = vals[++i], B = vals[i] }];
        //            }
        //        }
        //    }
        //    var tree = new ProblemTree<ABNode>() { Head = aNode };
        //    return tree;
        //}

        [HttpPost("Train")]
        public ActionResult<ProblemTree<ABNode>> PostABTrainVerify(ProblemTree<ABNode> tree)
        {
            AlphaBetaService.Search(tree);
            return tree;
        }

        [Authorize, HttpGet("Test")]
        public async Task<ActionResult<ProblemTree<ABNode>>> GetABTest()
        {
            var fp = await _context.AlphaBeta.FirstOrDefaultAsync(f => f.UserId == _userManager.GetUserId(User));
            if (fp == null)
            {
                return NotFound();
            }
            if (fp.Problem == null)
            {
                ABNode aNode = new ABNode();
                var tree = AlphaBetaService.GenerateTree(aNode, fp.TreeDepth);
                fp.Problem = tree.ToJson();

                fp.Solution = AlphaBetaService.GenerateSolution(tree).ToJson();

                _context.AlphaBeta.Update(fp);
                await _context.SaveChangesAsync();
                return tree;
            }
            return fp.Problem.FromJson<ProblemTree<ABNode>>();
        }

        [Authorize, HttpPost("Test")]
        public async Task<ActionResult<ProblemTree<ABNode>>> PostABTestVerify(ProblemTree<ABNode> tree)
        {
            var fp = await _context.AlphaBeta.FirstOrDefaultAsync(f => f.UserId == _userManager.GetUserId(User));
            if (fp == null)
            {
                return NotFound();
            }
            var (problem, solution) = (fp.Problem.FromJson<ProblemTree<ABNode>>(), fp.Solution.FromJson<ProblemTree<ABNode>>());
            if (problem != tree)
            {
                return BadRequest();
            }
            if (solution == null)
            {
                solution = AlphaBetaService.GenerateSolution(tree);
                fp.Solution = solution.ToJson();

            }
            fp.IsSolved = true;
            _context.AlphaBeta.Update(fp);
            await _context.SaveChangesAsync();
            return solution;
        }

        [Authorize(Roles = "Administrator"), HttpGet("Users/")]
        public ActionResult<AlphaBeta[]?> GetUsers()
        {
            var fp = _context.AlphaBeta.Where(f => f.User.Email != "admin@admin.com").ToArray();
            return fp;
        }

        [Authorize(Roles = "Administrator"), HttpGet("Users/{userId}/")]
        public ActionResult<AlphaBeta[]?> GetUser(string userId)
        {
            var fp = _context.AlphaBeta.Where(f => f.UserId == userId).ToArray();
            return fp;
        }

        [Authorize(Roles = "Administrator"), HttpPut("Users/{userId}/")]
        public async Task<ActionResult> UpdateABTest(string userId, [System.Web.Http.FromUri] int? depth = null, [System.Web.Http.FromUri] bool generate = false)
        {
            var fp = await _context.AlphaBeta.FirstOrDefaultAsync(f => f.UserId == userId);
            if (fp == null)
            {
                return NotFound();
            }
            if (depth != null)
            {
                fp.TreeDepth = (int)depth;
            }
            fp.Problem = null;
            fp.Solution = null;
            fp.IsSolved = false;
            if (generate == true)
            {
                ABNode aNode = new ABNode();
                var tree = AlphaBetaService.GenerateTree(aNode, fp.TreeDepth);
                fp.Problem = tree.ToJson();

                fp.Solution = AlphaBetaService.GenerateSolution(tree).ToJson();
            }
            _context.AlphaBeta.Update(fp);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [Authorize(Roles = "Administrator"), HttpDelete("Users/{userId}")]
        public async void DeleteABTest(string userId)
        {
            var fp = await _context.AlphaBeta.FirstOrDefaultAsync(f => f.UserId == userId);
            if (fp == null) { return; }
            fp.Problem = null;
            fp.Solution = null;
            fp.IsSolved = false;
            _context.AlphaBeta.Update(fp);
            _context.SaveChanges();
        }
    }
}
