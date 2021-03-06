﻿using System;

namespace TestProject
{
	[Serializable]
	public class MyException : Exception
	{
		// hover on following constructor methods to see XML Doc from type Exception
		public MyException() { }
		public MyException(string message) : base(message) { }
		public MyException(string message, Exception inner) : base(message, inner) { }
		protected MyException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}

	abstract class Base
	{
		/// <summary>
		/// Do something.
		/// </summary>
		protected abstract void Do();
	}
	interface IRun
	{
		/// <summary>
		/// Run for a while.
		/// </summary>
		void Run();
	}
	interface IWalk
	{
		/// <summary>
		/// Walk a minute.
		/// </summary>
		void Walk();
	}

	class TestInheritDoc : Base, IRun, IWalk
	{
		/// <summary>
		/// The description of method.
		/// </summary>
		/// <param name="param">The description of parameter.</param>
		public static void Method(string param) {
		}

		// turn on <inheritdoc cref=""/> feature in the options page of Super Quick Info,
		// hover on the following method to see it inherit doc from the above method
		/// <inheritdoc cref="Method(string)"/>
		/// <param name="value">The value of the method.</param>
		public void InheritFromMethod(string param, string value) {

		}

		// hover on Do to see its inherited documentation
		protected override void Do() {
		}

		// hover on Run to see its inherited documentation
		public void Run() {
		}
		// this does not inherited from documentation of IRun
		public void Run(int mile) { }

		// hover on Walk to see its inherited documentation
		void IWalk.Walk() {
		}

		// hover on ToString to see its inherited documentation
		public override string ToString() {
			return "TEST";
		}
	}
}
