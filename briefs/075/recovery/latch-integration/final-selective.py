# Rerun only the corrected selective variant after production-envelope rejection.
# No baseline/paving/swap rebuild; their inputs and geometry are unchanged.
exec(compile(open('briefs/075/recovery/latch-integration/chain.py').read().split('try:\n # No running Editor')[0], 'chain-definitions', 'exec'))
paving='Assets/Scenes/probe-oldtown-ours.unity';candidate='Assets/Scenes/probe-recovery075-foliage3.unity'
step('foliage3-apply',R+'.Apply','-world',W,'-recoveryFoliage','selective','-recoveryScene',paving,'-recoverySave',candidate)
manifest('foliage',candidate);shoot('foliage',candidate)
print('Final three-placement selective proof complete; no adoption',flush=True)
