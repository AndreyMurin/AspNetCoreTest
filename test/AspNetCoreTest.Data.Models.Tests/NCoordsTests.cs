using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AspNetCoreTest.Data.Models.Tests
{
    public class NCoordsTests
    {
        [Fact]
        public void TestCoords()
        {
            for (var lenx = 1; lenx < 10; lenx++)
            {
                for (var leny = 1; leny < 10; leny++)
                {
                    for (var lenz = 1; lenz < 10; lenz++)
                    {
                        //Assert.True(true);
                        for (var i = 0; i < lenx * leny * lenz; i++)
                        {
                            var c = new NCoords(i, lenx, leny, lenz);
                            Assert.True(i==c.ToSingle(lenx,leny));
                        }
                    }
                    //Assert.True(true);
                }
                //Assert.True(true);
            }
        }

        
        [Fact]
        public void TestInterlockedAdd()
        {
            int _state = 10;
            var res = Interlocked.Add(ref _state, 20);
            Assert.True(res == 30);
        }
    }
}
