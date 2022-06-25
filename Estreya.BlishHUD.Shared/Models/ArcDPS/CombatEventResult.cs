namespace Estreya.BlishHUD.Shared.Models.ArcDPS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum CombatEventResult
{
	NORMAL, // good physical hit
	CRIT, // physical hit was crit
	GLANCE, // physical hit was glance
	BLOCK, // physical hit was blocked eg. mesmer shield 4
	EVADE, // physical hit was evaded, eg. dodge or mesmer sword 2
	INTERRUPT, // physical hit interrupted something
	ABSORB, // physical hit was "invlun" or absorbed eg. guardian elite
	BLIND, // physical hit missed
	KILLINGBLOW, // hit was killing hit
	DOWNED, // hit was downing hit
}
