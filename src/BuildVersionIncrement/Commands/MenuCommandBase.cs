// ----------------------------------------------------------------------
// Project:     BuildVersionIncrement
// Module Name: MenuCommandBase.cs
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

namespace BuildVersionIncrement.Commands
{
	using System;
	using System.ComponentModel.Design;

	using Microsoft.VisualStudio.Shell;

	internal abstract class MenuCommandBase<TCommand> where TCommand : MenuCommand
	{
		public readonly Guid CommandSet = new Guid(Constants.COMMAND_SET);

		internal MenuCommandBase(Package package)
		{
			ServiceProvider = package ?? throw new ArgumentNullException(nameof(package));
			Initialise();
		}

		public abstract int CommandId { get; }

		protected IServiceProvider ServiceProvider { get; }

		protected abstract TCommand GetCommand(CommandID menuCommandId);

		protected virtual void InitialiseEvents(TCommand command) {}

		private void Initialise()
		{
			var commandService =
				ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
			if (commandService == null)
			{
				return;
			}
			var menuCommandId = new CommandID(CommandSet, CommandId);
			var menuItem = GetCommand(menuCommandId);

			InitialiseEvents(menuItem);

			commandService.AddCommand(menuItem);
		}
	}
}