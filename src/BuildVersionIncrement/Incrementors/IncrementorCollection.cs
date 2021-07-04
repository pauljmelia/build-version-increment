// ----------------------------------------------------------------------
// Project:     BuildVersionIncrement
// Module Name: IncrementorCollection.cs
// ----------------------------------------------------------------------
// Created and maintained by Paul J. Melia.
// Copyright © 2016 Paul J. Melia.
// All rights reserved.
// ----------------------------------------------------------------------
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT 
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR 
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT 
// HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT 
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, 
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY 
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT 
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// ----------------------------------------------------------------------

namespace BuildVersionIncrement.Incrementors
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;

	using Helpers;

	using Logging;

	internal class IncrementorCollection
	{
		private readonly Dictionary<string, IncrementorBase> _incrementors =
			new Dictionary<string, IncrementorBase>();

		public IncrementorCollection()
		{
			// Add the static null instance to the list. This ensures that property grid references the "correct" null. 
			_incrementors.Add(BuiltInIncrementors.None.Name, BuiltInIncrementors.None);
		}

		public int Count => _incrementors.Keys.Count;

		public IncrementorBase this[string name] => _incrementors.ContainsKey(name) ? _incrementors[name] : null;

		public void AddFrom(Assembly asm)
		{
			Logger.Write($"Locating incrementors in assembly \"{asm.FullName}\" ...", LogLevel.Debug);
			var types = ReflectionHelper.GetTypesThatDeriveFromType(asm,
			                                                        typeof(IncrementorBase),
			                                                        false,
			                                                        false);

			Logger.Write($"Located {types.Count} incrementors.", LogLevel.Debug);

			foreach (var t in types.Where(t => t != typeof(BuiltInIncrementors.NoneIncrementor))) {
				Logger.Write($"Creating instance of incrementor type \"{t.FullName}\".", LogLevel.Debug);
				var incrementor = (IncrementorBase)Activator.CreateInstance(t);

				_incrementors.Add(incrementor.Name, incrementor);
			}
		}

		public string[] GetIncrementorNames()
		{
			var ret = new string[Count];

			_incrementors.Keys.CopyTo(ret, 0);

			return ret;
		}

		public IncrementorBase[] GetIncrementors()
		{
			var ret = new IncrementorBase[Count];

			_incrementors.Values.CopyTo(ret, 0);

			return ret;
		}
	}
}