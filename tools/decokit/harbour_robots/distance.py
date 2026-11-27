"""Far stage seen below six percent of the screen height. Fixed joints share the body draw."""
from geometry import Machine, CHROME, CREAM, IVORY, CORAL, MINT, TYRE, INK, LEATHER, PETAL, LEAF, BRASS, STEEL, WOOD, PAPER, panel_screen
from porter import arm_outline


def distant(tool):
    machine = Machine(3)
    porter = tool == 'Porter'
    machine.scale = 1.05 if porter else 1.1
    p = machine.part(tool + '_Silhouette')
    if porter:
        p.shell([(.10,.29,.74,0,-.05,INK),(.15,.34,.80,0,-.035,CORAL),
                 (.23,.32,.76,0,-.02,CORAL)],.43,sides=10)
        p.side_casting([(-.45,.16),(-.13,.30),(.06,.62),(.30,.81),(.54,.82),
                        (.76,.68),(.86,.42),(.81,.16)],(-.337,.337),CORAL)
        p.shell([(.69,.27,.30,0,.41,IVORY),(.80,.245,.255,0,.44,IVORY),
                 (.86,.17,.17,0,.455,CREAM)],.72,sides=10)
        for sign in (-1,1):
            x=sign*.36
            p.spindle((x-sign*.065,.5,.337),(x+sign*.086,.5,.337),
                      [(0,.29,TYRE),(.10,.335,TYRE),(.75,.335,TYRE),(.96,.25,TYRE),(1,.20,CHROME)],12)
            for z in (.371,.506):
                outline=arm_outline((.118,z),(-.477,z-.142),.036,2)
                p.face([(sign*.351,y,q) for y,q in (outline if sign>0 else outline[::-1])],CHROME)
        p.spindle((-.052,-.67,.103),(.052,-.67,.103),[(0,.102,TYRE),(1,.102,TYRE)],8)
        p.shell([(.218,.34,.51,0,-.5,INK),(.242,.384,.539,0,-.493,CHROME),
                 (.279,.376,.53,0,-.493,CHROME),(.292,.36,.522,0,-.493,WOOD)],.39,sides=12)
        p.shell([(.294,.26,.144,0,-.266,LEATHER),(.56,.26,.144,0,-.266,LEATHER),
                 (.585,.244,.128,0,-.266,LEATHER)],.28,sides=10)
        p.spindle((0,.443,.845),(0,.443,1.101),[(0,.115,STEEL),(.14,.093,CHROME),(1,.088,STEEL)],8)
        p.shell([(1.078,.28,.14,0,.44,STEEL),(1.115,.375,.175,0,.44,IVORY),
                 (1.216,.386,.18,0,.44,IVORY),(1.282,.275,.129,0,.45,CREAM)],.54,sides=12)
        panel_screen(p,'Porter_Head',.253,1.17,.625,.112)
    else:
        p.shell([(.19,.265,.235,0,.013,INK),(.218,.323,.265,0,.005,CHROME),
                 (.242,.324,.265,0,.005,MINT),(.49,.24,.201,0,.03,MINT),
                 (.784,.109,.101,0,.04,MINT)],sides=12)
        for x,y in [(-.219,-.105),(.219,-.105),(0,.184)]:
            p.rod((x,y,.126),(x,y,.22),.030,CHROME,5)
            p.spindle((x-.037,y,.082),(x+.037,y,.082),[(0,.068,TYRE),(.55,.082,TYRE),(1,.066,STEEL)],6)
        p.spindle((0,.04,.78),(0,.03,.932),[(0,.112,BRASS),(.18,.123,INK),(1,.124,INK)],8)
        p.shell([(.916,.12,.10,0,.02,STEEL),(.973,.166,.13,0,.017,CREAM),
                 (1.08,.198,.145,0,.028,IVORY),(1.15,.114,.083,0,.045,IVORY)],.81,sides=9)
        p.rod((0,.04,1.14),(0,.033,1.28),.058,STEEL,7)
        p.shell([(1.254,.12,.105,0,.027,STEEL),(1.29,.169,.154,0,.018,INK),
                 (1.37,.182,.182,0,.01,INK)],.80,tilt=-.20,sides=10)
        p.shell([(1.318,.164,.17,0,.032,BRASS),(1.34,.203,.224,0,.009,IVORY),
                 (1.399,.186,.196,0,.037,IVORY),(1.485,.088,.09,0,.071,CREAM)],.86,tilt=-.43,sides=10)
        panel_screen(p,'Clerk_Head',-.173,1.357,.271,.061)
        for sign,elbow,wrist in [(1,(.285,-.078,.858),(.166,-.353,.822)),(-1,(-.31,-.078,.954),(-.22,-.298,1.129))]:
            p.rod((sign*.195,.028,1.073),elbow,.044,STEEL,7)
            p.rod(elbow,wrist,.030,CHROME,6)
            p.rod(wrist,(wrist[0],wrist[1]-.06,wrist[2]+.04),.021,STEEL,4)
        p.shell([(.842,.049,.045,.146,-.416,STEEL),(.877,.14,.119,.146,-.416,CHROME),
                 (.922,.163,.141,.146,-.416,BRASS)],sides=9)
        bloom=machine.part('Clerk_Flowers',parent=p)
        for x,y,z in [(.105,-.436,1),(.216,-.410,1.02),(.154,-.496,.984),(-.277,-.386,1.299)]:
            a=(-.222,-.365,1.143) if x<0 else (.146,-.416,.922)
            bloom.rod(a,(x,y,z),.006,LEAF,4)
            bloom.face([(x-.038,y,z),(x,y-.044,z+.006),(x+.042,y,z),(x,y+.036,z+.015)],PETAL)
            bloom.face([(x-.031,y,z+.013),(x,y,z-.012),(x+.034,y,z+.018)],PETAL)
        pages=machine.part('Clerk_Papers',parent=p)
        pages.shell([(.923,.106,.056,.146,-.413,PAPER),(.955,.101,.05,.146,-.413,PAPER)],.30,sides=8)
    return machine
