<!-- Duplicated from ume-rg-myplatform/src/ume-stapp-minasidor/src/components/base/BaseAnimatedSuccess.vue @ 84b4a5dc -->
<template>
	<div class="sa">
		<div class="sa-success">
			<div class="sa-success-tip"></div>
			<div class="sa-success-long"></div>
			<div class="sa-success-placeholder"></div>
			<div class="sa-success-fix"></div>
		</div>
	</div>
</template>

<style scoped lang="scss">
.sa {
	.sa-success-tip {
		background: $primary;
	}
	.sa-success-long {
		background: $primary;
	}

	&-success {
		border-radius: 50%;
		border: 8px solid $primary;
		box-sizing: content-box;
		height: 80px;
		padding: 0;
		position: relative;
		width: 80px;
		&:after,
		&:before {
			content: '';
			height: 120px;
			position: absolute;
			transform: rotate(45deg);
			width: 60px;
		}
		&:before {
			border-radius: 40px 0 0 40px;
			width: 26px;
			height: 80px;
			top: -17px;
			left: 5px;
			transform-origin: 60px 60px;
			transform: rotate(-45deg);
		}
		&:after {
			border-radius: 0 120px 120px 0;
			left: 30px;
			top: -11px;
			transform-origin: 0 60px;
			transform: rotate(-45deg);
			animation: rotatePlaceholder 4.25s ease-in;
		}
		&-placeholder {
			border-radius: 50%;
			box-sizing: content-box;
			height: 80px;
			left: -4px;
			position: absolute;
			top: -4px;
			width: 80px;
			z-index: 2;
		}
		&-fix {
			height: 90px;
			left: 28px;
			position: absolute;
			top: 8px;
			transform: rotate(-45deg);
			width: 5px;
			z-index: 1;
		}
		&-tip,
		&-long {
			background-color: #fff;
			border-radius: 2px;
			height: 5px;
			position: absolute;
			z-index: 2;
		}
		&-tip {
			left: 14px;
			top: 46px;
			transform: rotate(45deg);
			width: 25px;
			animation: animateSuccessTip 0.75s;
		}
		&-long {
			right: 8px;
			top: 38px;
			transform: rotate(-45deg);
			width: 47px;
			animation: animateSuccessLong 0.75s;
		}
	}
}
@keyframes animateSuccessTip {
	0%,
	54% {
		width: 0;
		left: 1px;
		top: 19px;
	}
	70% {
		width: 50px;
		left: -8px;
		top: 37px;
	}
	84% {
		width: 17px;
		left: 21px;
		top: 48px;
	}
	100% {
		width: 25px;
		left: 14px;
		top: 45px;
	}
}
@keyframes animateSuccessLong {
	0%,
	65% {
		width: 0;
		right: 46px;
		top: 54px;
	}
	84% {
		width: 55px;
		right: 0;
		top: 35px;
	}
	100% {
		width: 47px;
		right: 8px;
		top: 38px;
	}
}
@keyframes rotatePlaceholder {
	0%,
	5% {
		transform: rotate(-45deg);
	}
	100%,
	12% {
		transform: rotate(-405deg);
	}
}
</style>
