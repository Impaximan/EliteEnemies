using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.GameInput;

namespace EliteEnemies
{
    class EliteNPC : GlobalNPC
    {
        public bool elite = false;

        public static EliteNPC eliteNPC(NPC npc)
        {
            return npc.GetGlobalNPC<EliteNPC>();
        }

        public override bool InstancePerEntity => true;


        List<int> bannedNPCs = new List<int>();
        public override void SetDefaults(NPC npc)
        {
            bannedNPCs.Add(NPCID.TheDestroyerBody);
            bannedNPCs.Add(NPCID.TheDestroyerTail);
            bannedNPCs.Add(NPCID.EaterofWorldsBody);
            bannedNPCs.Add(NPCID.EaterofWorldsHead);
            bannedNPCs.Add(NPCID.EaterofWorldsTail);
            bannedNPCs.Add(NPCID.ServantofCthulhu);
            bannedNPCs.Add(NPCID.GolemHead);
            bannedNPCs.Add(NPCID.GolemHeadFree);
            bannedNPCs.Add(NPCID.GolemFistLeft);
            bannedNPCs.Add(NPCID.GolemFistRight);
            bannedNPCs.Add(NPCID.PirateCaptain);
            bannedNPCs.Add(NPCID.Probe);

            if (!npc.townNPC && !npc.boss && Main.rand.Next(125) <= 0 && npc.aiStyle != 6 && !bannedNPCs.Contains(npc.type))
            {
                elite = true;
            }
            if (elite)
            {
                BecomeElite(npc);
            }
        }

        public void DustCircle(Vector2 position, int Dusts, float Radius, int DustType, float DustSpeed, float DustScale = 1f) //Thanks to seraph for this code
        {
            float currentAngle = Main.rand.Next(360);
            for (int i = 0; i < Dusts; ++i)
            {

                Vector2 direction = Vector2.Normalize(new Vector2(1, 1)).RotatedBy(MathHelper.ToRadians(((360 / Dusts) * i) + currentAngle));
                direction.X *= Radius;
                direction.Y *= Radius;

                Dust dust = Dust.NewDustPerfect(position + direction, DustType, (direction / Radius) * DustSpeed, 0, default(Color), DustScale);
                dust.noGravity = true;
                dust.noLight = true;
                dust.alpha = 125;
            }
        }

        int healAuraCooldown = 30;
        bool healer = false;
        int summonCooldown = 240;
        bool smolstart = true;
        public override void AI(NPC npc)
        {

            if (elite)
            {
                if (!invis)
                {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, DustID.GoldFlame);
                    Main.dust[dust].velocity = npc.velocity;
                    Main.dust[dust].noLight = true;
                    Main.dust[dust].noGravity = true;
                }

                if (Main.player[npc.target].dead || !Main.player[npc.target].active)
                {
                    npc.active = false;
                    DustExplosion(npc.Center, 0, 80, 20, DustID.GoldFlame, NoGravity: true);
                }
            }
            if (smol)
            {
                npc.scale = 0.5f;
                npc.dontCountMe = true;
                if (smolstart)
                {
                    smolstart = false;
                    npc.lifeMax /= 10;
                    npc.life = npc.lifeMax;
                }
            }
            if (healer && !smol)
            {
                healAuraCooldown--;
                if (healAuraCooldown <= 0)
                {
                    healAuraCooldown = 120;
                    int range = 300;
                    int enemyCount = 0;
                    DustCircle(npc.Center, 180, range, 235, -5, 3f);
                    for (int i = 0; i < 200; i++)
                    {
                        NPC target = Main.npc[i];
                        if (target.active && target.Distance(npc.Center) <= range && !target.friendly && !target.boss && target != npc && !eliteNPC(target).smol)
                        {
                            enemyCount++;
                            target.HealEffect(target.lifeMax / 10);
                            target.life += target.lifeMax / 10;
                            if (target.life >= target.lifeMax)
                            {
                                target.life = target.lifeMax;
                            }
                        }
                    }
                    npc.HealEffect((npc.lifeMax / 3) * enemyCount + npc.lifeMax / 50);
                    npc.life += (npc.lifeMax / 3) * enemyCount + npc.lifeMax / 50;
                    if (npc.life >= npc.lifeMax)
                    {
                        npc.life = npc.lifeMax;
                    }
                }
            }
            if (summoner && !smol)
            {
                summonCooldown--;
                if (summonCooldown <= 0)
                {
                    summonCooldown = 240;
                    int spawnedNPC = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, npc.type);
                    EliteNPC spawned = eliteNPC(Main.npc[spawnedNPC]);
                    spawned.smol = true;
                }
            }
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (bouncy)
            {
                damage *= 2;
            }
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref int damage, ref float knockback, ref bool crit)
        {
            if (bouncy)
            {
                damage *= 5;
            }
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, int damage, float knockback, bool crit)
        {
            if (bouncy)
            {
                Main.PlaySound(SoundID.Item56);
                projectile.hostile = true;
                projectile.friendly = false;
                projectile.velocity *= -1;
                projectile.timeLeft = 600;
                projectile.penetrate = -1;
                projectile.damage = npc.damage / 2;
            }
            if (splitter)
            {
                int spawnedNPC = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, npc.type);
                EliteNPC spawned = eliteNPC(Main.npc[spawnedNPC]);
                spawned.smol = true;
            }
        }

        public override void OnHitByItem(NPC npc, Player player, Item item, int damage, float knockback, bool crit)
        {
            if (bouncy)
            {
                Main.PlaySound(SoundID.Item56);
                player.velocity = npc.DirectionTo(player.Center) * 20;
            }
            if (splitter)
            {
                int spawnedNPC = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, npc.type);
                EliteNPC spawned = eliteNPC(Main.npc[spawnedNPC]);
                spawned.smol = true;
            }
        }

        public int statModifier1 = 0;
        public int statModifier2 = 0;
        public bool bouncy = false;
        public bool smol = false;
        bool invis = false;
        bool splitter = false;
        bool summoner = false;

        int modfierCount = 7;

        public void BecomeElite(NPC npc)
        {
            npc.lifeMax *= 10;
            npc.life = npc.lifeMax;
            npc.value *= 100;

            statModifier1 = Main.rand.Next(modfierCount);
            addModifier(statModifier1, npc);

            if (NPC.downedBoss2 || Main.hardMode)
            {
                statModifier2 = Main.rand.Next(modfierCount);
                while (statModifier2 == statModifier1)
                {
                    statModifier2 = Main.rand.Next(4);
                }
                addModifier(statModifier2, npc);
            }
        }

        void addModifier(int number, NPC npc)
        {
            if (number == 0)
            {
                if (npc.noGravity)
                {
                    npc.scale *= 2;
                    npc.noTileCollide = true;
                    npc.damage *= 2;
                }
                else
                {
                    npc.alpha = (int)(255 * 0.90f);
                    npc.damage *= 3;
                    invis = true;
                }
            }
            if (number == 1)
            {
                bouncy = true;
                npc.knockBackResist *= 4;
            }
            else
            {
                npc.knockBackResist = 0;
            }
            if (number == 2)
            {
                splitter = true;
                npc.lifeMax /= 3;
            }
            if (number == 3)
            {
                npc.lifeMax /= 2;
                npc.life = npc.lifeMax;
                healer = true;
            }
            if (number == 4)
            {
                npc.lifeMax *= 2;
                npc.damage /= 4;
                npc.life = npc.lifeMax;
            }
            if (number == 5)
            {
                npc.lifeMax /= 2;
                npc.damage *= 4;
                npc.life = npc.lifeMax;
            }
            if (number == 6)
            {
                summoner = true;
                npc.lifeMax /= 2;
            }
        }

        public void DustExplosion(Vector2 position, int RectWidth, int Streams, float DustSpeed, int DustType, float DustScale = 1f, bool NoGravity = false) //Thank you once again Seraph
        {
            float currentAngle = Main.rand.Next(360);

            //if(Main.netMode!=1){
            for (int i = 0; i < Streams; ++i)
            {

                Vector2 direction = Vector2.Normalize(new Vector2(1, 1)).RotatedBy(MathHelper.ToRadians(((360 / Streams) * i) + currentAngle));
                direction.X *= DustSpeed;
                direction.Y *= DustSpeed;

                Dust dust = Dust.NewDustPerfect(position + (new Vector2(Main.rand.Next(RectWidth), Main.rand.Next(RectWidth))), DustType, direction, 0, default(Color), DustScale);
                if (NoGravity)
                {
                    dust.noGravity = true;
                }
            }
        }
    }
}
