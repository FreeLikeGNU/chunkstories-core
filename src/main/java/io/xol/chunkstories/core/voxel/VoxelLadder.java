package io.xol.chunkstories.core.voxel;

import io.xol.chunkstories.api.physics.CollisionBox;
import io.xol.chunkstories.api.voxel.Voxel;
import io.xol.chunkstories.api.voxel.VoxelType;
import io.xol.chunkstories.api.voxel.models.VoxelModel;
import io.xol.chunkstories.api.world.VoxelContext;

public class VoxelLadder extends Voxel implements VoxelClimbable
{
	VoxelModel[] models = new VoxelModel[4];

	public VoxelLadder(VoxelType type)
	{
		super(type);
		for (int i = 0; i < 4; i++)
			models[i] = store.models().getVoxelModelByName("dekal.m" + i);
	}

	@Override
	public VoxelModel getVoxelRenderer(VoxelContext info)
	{
		int meta = info.getMetaData();
		
		if(meta == 2)
			return models[2];
		else if(meta == 3)
			return models[3];
		else if(meta == 4)
			return models[0];
		else if(meta == 5)
			return models[1];
		return models[0];
	}

	@Override
	public CollisionBox[] getCollisionBoxes(VoxelContext info) {

		int meta = info.getMetaData();
		
		if(meta == 2)
			return new CollisionBox[]{new CollisionBox(1.0, 1.0, 0.1).translate(0.0, 0.0, 0.9)};
		if(meta == 3)
			return new CollisionBox[]{new CollisionBox(1.0, 1.0, 0.1)};
		if(meta == 4)
			return new CollisionBox[]{new CollisionBox(0.1, 1.0, 1.0).translate(0.9, 0.0, 0.0)};
		if(meta == 5)
			return new CollisionBox[]{new CollisionBox(0.1, 1.0, 1.0)};
		
		return super.getCollisionBoxes(info);
	}
	
	
}
